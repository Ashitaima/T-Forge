"""Профілі навчальних датасетів — єдине визначення схеми.

І `train.py --validate`, і сам тренувальний шлях ходять через цей модуль, тож
правила читання CSV існують в одному місці. Esport-Backend ніколи не розбирає
CSV самотужки: він питає `train.py --validate` і читає JSON-вердикт. Друга копія
цих правил на C# була б третім дзеркальним контрактом поряд із ознаками й
класами, і ніщо б її не тримало.

features.py цей модуль імпортує, але не навпаки: там живе контракт ознак, який
дзеркалить .NET, і він не має залежати від того, звідки прийшли рядки.
"""
from typing import Iterable

import pandas as pd

from features import TIME_CONTROLS, outcome_class, parse_increment_code

LICHESS = "Lichess"
MINIMAL = "Minimal"

# Повний набір колонок Kaggle-вибірки Lichess. Усі вісім — інакше профіль
# мінімальний, і фільтри rated/turns просто не застосовуються.
LICHESS_COLUMNS = ["id", "rated", "turns", "created_at", "winner",
                   "increment_code", "white_rating", "black_rating"]

MINIMAL_COLUMNS = ["white_rating", "black_rating", "winner"]

WINNERS = ("white", "draw", "black")

# GridSearchCV із TimeSeriesSplit(5) по 80 % рядків потребує, щоб у кожному
# фолді були всі три класи. Нижче кількох сотень партій метрики — шум, і
# відмовити одразу чесніше, ніж віддати впевнену неправильну модель.
MIN_ROWS = 200


def detect_profile(columns: Iterable[str]) -> str:
    return LICHESS if set(LICHESS_COLUMNS) <= set(columns) else MINIMAL


def missing_columns(columns: Iterable[str]) -> list[str]:
    """Яких обов'язкових колонок бракує. У Lichess-профілі — за визначенням жодної."""
    present = set(columns)
    missing = [column for column in MINIMAL_COLUMNS if column not in present]
    if "time_control" not in present and "increment_code" not in present:
        missing.append("time_control or increment_code")
    return missing


def _time_control(df: pd.DataFrame) -> pd.Series:
    if "time_control" in df.columns:
        return df["time_control"].astype(str).str.strip()
    return df["increment_code"].astype(str).map(parse_increment_code)


def _order_column(df: pd.DataFrame, profile: str) -> str | None:
    column = "created_at" if profile == LICHESS else "played_at"
    return column if column in df.columns else None


def normalize(df: pd.DataFrame) -> tuple[pd.DataFrame, str, bool]:
    """Рядки, готові до навчання, плюс профіль і чи вдалося впорядкувати за часом.

    Кидає ValueError зі зрозумілим повідомленням на все непридатне: --validate
    перетворює його на issue, а навчання — на причину збою.
    """
    profile = detect_profile(df.columns)

    missing = missing_columns(df.columns)
    if missing:
        raise ValueError(f"відсутні колонки: {', '.join(missing)}")

    df = df.copy()

    if profile == LICHESS:
        df = df.drop_duplicates(subset="id")
        df = df[df["rated"].astype(str).str.lower() == "true"]
        df = df[pd.to_numeric(df["turns"], errors="coerce").fillna(0) >= 2]

    df["time_control"] = _time_control(df)
    unknown_tc = sorted(set(df.loc[~df["time_control"].isin(TIME_CONTROLS), "time_control"]))
    if unknown_tc:
        raise ValueError(f"невідомий контроль часу: {', '.join(unknown_tc[:5])}")

    df["winner"] = df["winner"].astype(str).str.strip().str.lower()
    unknown_winner = sorted(set(df.loc[~df["winner"].isin(WINNERS), "winner"]))
    if unknown_winner:
        raise ValueError(f"невідоме значення winner: {', '.join(unknown_winner[:5])}")

    df["y"] = df["winner"].map(outcome_class)

    for column in ("white_rating", "black_rating"):
        df[column] = pd.to_numeric(df[column], errors="coerce")
    df = df.dropna(subset=["white_rating", "black_rating"])

    order = _order_column(df, profile)
    is_time_ordered = order is not None
    if order is not None:
        df[order] = (pd.to_numeric(df[order], errors="coerce") if order == "created_at"
                     else pd.to_datetime(df[order], errors="coerce", format="mixed"))
        df = df.dropna(subset=[order])
        keys = [order, "id"] if "id" in df.columns else [order]
        df = df.sort_values(keys, kind="stable")

    if len(df) < MIN_ROWS:
        raise ValueError(f"замало рядків після фільтрів: {len(df)}, потрібно щонайменше {MIN_ROWS}")

    return df.reset_index(drop=True), profile, is_time_ordered


def summarize(df: pd.DataFrame, profile: str, is_time_ordered: bool) -> dict:
    return {
        "profile": profile,
        "rows": int(len(df)),
        "by_time_control": {tc: int((df["time_control"] == tc).sum()) for tc in TIME_CONTROLS},
        "is_time_ordered": bool(is_time_ordered),
        "issues": [],
    }
