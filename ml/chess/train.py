"""Train, compare and export the two chess result models.

    python train.py                     # data/raw/games.csv
    python train.py --data other.csv    # e.g. a filtered Lichess monthly dump with the same columns

The functions are importable — chess_prediction.ipynb uses them instead of
re-implementing the pipeline.
"""
import argparse
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import onnx.helper
import onnxruntime as ort
import pandas as pd
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType
from sklearn.ensemble import HistGradientBoostingClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score, log_loss, recall_score
from sklearn.model_selection import GridSearchCV, TimeSeriesSplit
from sklearn.pipeline import make_pipeline
from sklearn.preprocessing import StandardScaler

from datasets import normalize, summarize
from features import (CLASS_ORDER, FEATURE_ORDER, TIME_CONTROLS, elo_baseline,
                      feature_vector)

def _ints_not_bools(make_attribute):
    """skl2onnx writes HistGradientBoosting's `nodes_missing_value_tracks_true`
    as a mix of uint8 and Python bools; onnx >= 1.18 rejects bools in an INTS
    attribute. Casting bool -> int is lossless (False == 0, True == 1)."""
    def wrapper(key, value, *args, **kwargs):
        if isinstance(value, (list, tuple)) and any(isinstance(v, bool) for v in value):
            value = [int(v) for v in value]
        return make_attribute(key, value, *args, **kwargs)
    return wrapper


onnx.helper.make_attribute = _ints_not_bools(onnx.helper.make_attribute)

HERE = Path(__file__).parent
REPO = HERE.parent.parent
MODELS_DIR = REPO / "Esport-Backend" / "MlModels" / "chess"
FIXTURES_DIR = REPO / "Esport-Backend.Tests" / "Fixtures"
OUT = HERE / "out"
DEFAULT_DATA = HERE / "data" / "raw" / "games.csv"


def load_dataset(path: Path) -> tuple[pd.DataFrame, str, bool]:
    """Рядки, профіль і чи впорядковано за часом. Правила — у datasets.py."""
    return normalize(pd.read_csv(path))


def load(path: Path) -> pd.DataFrame:
    """Лише рядки.

    Підпис лишається таким, яким був, бо build_notebook.py робить
    `df = train.load(train.DEFAULT_DATA)` і генерує з нього
    chess_prediction.ipynb — текст курсової. Профіль і впорядкованість
    віддає load_dataset.
    """
    return load_dataset(path)[0]


def validate(path: Path) -> dict:
    """Вердикт для --validate: або підсумок, або непорожній issues.

    Будь-яка проблема саме з файлом (немає колонки, порожній, не парситься)
    лишається нульовим кодом виходу й непорожнім issues. Ненульовий код чи
    нерозбірливий stdout означає зламаний інструментарій — і бекенд розрізняє
    ці два випадки саме так.
    """
    empty = {"profile": None, "rows": 0, "by_time_control": {}, "is_time_ordered": False}
    try:
        rows, profile, is_time_ordered = load_dataset(path)
    except (ValueError, OSError, pd.errors.ParserError, pd.errors.EmptyDataError) as problem:
        return {**empty, "issues": [str(problem) or problem.__class__.__name__]}
    return summarize(rows, profile, is_time_ordered)


def matrix(df: pd.DataFrame) -> np.ndarray:
    return np.array([feature_vector(w, b, tc) for w, b, tc in
                     zip(df["white_rating"], df["black_rating"], df["time_control"])], dtype=np.float32)


def split(df: pd.DataFrame, train_share: float = 0.8):
    cut = int(len(df) * train_share)
    return df.iloc[:cut], df.iloc[cut:]


def brier(y: np.ndarray, p: np.ndarray) -> float:
    onehot = np.eye(3)[y]
    return float(np.mean(np.sum((p - onehot) ** 2, axis=1)))


def evaluate(y: np.ndarray, p: np.ndarray) -> dict:
    pred = p.argmax(axis=1)
    return {
        "log_loss": float(log_loss(y, p, labels=[0, 1, 2])),
        "brier": brier(y, p),
        "accuracy": float(accuracy_score(y, pred)),
        "draw_recall": float(recall_score(y, pred, labels=[1], average="macro", zero_division=0)),
    }


def fit_logreg(x: np.ndarray, y: np.ndarray, cv) -> object:
    search = GridSearchCV(
        make_pipeline(StandardScaler(), LogisticRegression(max_iter=2000)),
        {"logisticregression__C": [0.01, 0.1, 1.0, 10.0]},
        cv=cv, scoring="neg_log_loss")
    return search.fit(x, y)


def fit_gbt(x: np.ndarray, y: np.ndarray, cv) -> object:
    search = GridSearchCV(
        HistGradientBoostingClassifier(random_state=42),
        # The first grid (lr 0.03/0.1, depth 3/5, iter 100/300) chose its lower
        # corner on every axis, so it was extended downwards. Chosen by CV only.
        {"learning_rate": [0.01, 0.03, 0.1], "max_depth": [2, 3, 5], "max_iter": [50, 100, 300]},
        cv=cv, scoring="neg_log_loss")
    return search.fit(x, y)


def elo_probs(df: pd.DataFrame, draw_rate: float) -> np.ndarray:
    return np.array([elo_baseline(w, b, draw_rate)
                     for w, b in zip(df["white_rating"], df["black_rating"])])


def to_onnx(model, path: Path) -> None:
    onx = convert_sklearn(model, initial_types=[("input", FloatTensorType([None, len(FEATURE_ORDER)]))],
                          options={id(model): {"zipmap": False}}, target_opset=17)
    path.write_bytes(onx.SerializeToString())
    outputs = [o.name for o in ort.InferenceSession(str(path)).get_outputs()]
    assert "probabilities" in outputs, f"{path.name}: expected a 'probabilities' output, got {outputs}"


def onnx_probs(path: Path, x: np.ndarray) -> np.ndarray:
    return ort.InferenceSession(str(path)).run(["probabilities"], {"input": x})[0]


FIXTURE_PAIRS = [(1500, 1500), (1800, 1400), (1200, 2000), (2400, 2350), (900, 900),
                 (2600, 1100), (1650, 1700), (1100, 1000), (2000, 2000), (1450, 1900),
                 (1300, 1250), (2200, 1800)]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", default=str(DEFAULT_DATA))
    parser.add_argument("--validate", action="store_true",
                        help="Перевірити файл і вивести JSON-вердикт; нічого не навчати")
    parser.add_argument("--out", default=str(OUT),
                        help="Куди писати артефакти (типово ml/chess/out)")
    parser.add_argument("--no-install",
                        dest="install", action="store_false", default=True,
                        help="Не копіювати результат у MlModels/chess і Fixtures — саме це "
                             "застосунок передає завжди, щоб не зачепити вбудовану модель")
    args = parser.parse_args()

    if args.validate:
        print(json.dumps(validate(Path(args.data)), ensure_ascii=False))
        return

    out = Path(args.out)
    df, profile, is_time_ordered = load_dataset(Path(args.data))
    train, test = split(df)
    x_train, y_train = matrix(train), train["y"].to_numpy()
    x_test, y_test = matrix(test), test["y"].to_numpy()
    cv = TimeSeriesSplit(n_splits=5)

    logreg_search = fit_logreg(x_train, y_train, cv)
    gbt_search = fit_gbt(x_train, y_train, cv)
    logreg, gbt = logreg_search.best_estimator_, gbt_search.best_estimator_

    draw_rate = float((y_train == 1).mean())
    metrics = {
        "logreg": evaluate(y_test, logreg.predict_proba(x_test)),
        "gbt": evaluate(y_test, gbt.predict_proba(x_test)),
        "elo": evaluate(y_test, elo_probs(test, draw_rate)),
    }

    out.mkdir(parents=True, exist_ok=True)

    to_onnx(logreg, out / "logreg.onnx")
    to_onnx(gbt, out / "gbt.onnx")

    info = {
        "trained_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "dataset": Path(args.data).name,
        "profile": profile,
        # False — у файлі не було колонки впорядкування, тож спліт позиційний, а
        # не хронологічний. Метрики від цього не стають хибними, але й
        # «передбаченням майбутнього» вже не є, і UI це показує.
        "is_time_ordered": is_time_ordered,
        "rows_train": int(len(train)),
        "rows_test": int(len(test)),
        "feature_order": FEATURE_ORDER,
        "class_order": CLASS_ORDER,
        "elo_draw_rate": draw_rate,
        # How many training games each time control had. The Kaggle set is almost
        # all Rapid/Classical, so the app flags predictions for thin categories.
        "train_games_by_time_control": {tc: int((train["time_control"] == tc).sum()) for tc in TIME_CONTROLS},
        "best_params": {"logreg": logreg_search.best_params_, "gbt": gbt_search.best_params_},
        "metrics": metrics,
    }
    (out / "model_info.json").write_text(json.dumps(info, indent=2), encoding="utf-8")
    (out / "metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")

    # Parity fixtures: every time control, a spread of ratings, both extremes.
    # Probabilities come from the ONNX files, not predict_proba — the .NET side
    # runs the ONNX graph, so that is what it must agree with.
    rows = [(w, b, tc) for tc in TIME_CONTROLS for w, b in FIXTURE_PAIRS]
    x_fix = np.array([feature_vector(w, b, tc) for w, b, tc in rows], dtype=np.float32)
    p_log = onnx_probs(out / "logreg.onnx", x_fix)
    p_gbt = onnx_probs(out / "gbt.onnx", x_fix)
    fixtures = [{"white": w, "black": b, "time_control": tc,
                 "logreg": [float(v) for v in p_log[i]], "gbt": [float(v) for v in p_gbt[i]],
                 "elo": list(elo_baseline(w, b, draw_rate))}
                for i, (w, b, tc) in enumerate(rows)]
    (out / "parity_fixtures.json").write_text(json.dumps(fixtures, indent=2), encoding="utf-8")

    # Застосунок завжди передає --no-install: саме це робить структурно
    # неможливим перезапис вбудованої моделі й parity-фікстури навчанням з UI.
    if args.install:
        for directory in (MODELS_DIR, FIXTURES_DIR):
            directory.mkdir(parents=True, exist_ok=True)
        for name in ("logreg.onnx", "gbt.onnx", "model_info.json"):
            shutil.copy(out / name, MODELS_DIR / name)
        shutil.copy(out / "parity_fixtures.json", FIXTURES_DIR / "chess_parity.json")

    print(f"train={len(train)} test={len(test)} draw_rate={draw_rate:.4f}")
    print("best params:", json.dumps(info["best_params"]))
    print(json.dumps(metrics, indent=2))


if __name__ == "__main__":
    main()
