"""Схема датасету має одне визначення — у datasets.py. Ці тести його і тримають."""
import pandas as pd
import pytest

from datasets import (LICHESS, MIN_ROWS, MINIMAL, detect_profile,
                      missing_columns, normalize, summarize)

LICHESS_ROW = {
    "id": "a", "rated": "TRUE", "created_at": 1.5e12, "turns": 20,
    "winner": "white", "increment_code": "15+2",
    "white_rating": 1500, "black_rating": 1400,
}

MINIMAL_ROW = {
    "white_rating": 1500, "black_rating": 1400,
    "winner": "white", "time_control": "Rapid",
}


def frame(row: dict, n: int = MIN_ROWS, **overrides) -> pd.DataFrame:
    rows = []
    for i in range(n):
        item = {**row, **overrides}
        if "id" in item:
            item["id"] = f"{item['id']}{i}"
        rows.append(item)
    return pd.DataFrame(rows)


# ---- Розпізнавання профілю ----

def test_detect_profile_lichess():
    assert detect_profile(LICHESS_ROW.keys()) == LICHESS


def test_detect_profile_minimal():
    assert detect_profile(MINIMAL_ROW.keys()) == MINIMAL


def test_detect_profile_lichess_missing_one_column_is_minimal():
    columns = set(LICHESS_ROW.keys()) - {"turns"}
    assert detect_profile(columns) == MINIMAL


# ---- Обов'язкові колонки ----

def test_missing_columns_none_for_minimal():
    assert missing_columns(MINIMAL_ROW.keys()) == []


def test_missing_columns_reports_winner():
    columns = set(MINIMAL_ROW.keys()) - {"winner"}
    assert "winner" in missing_columns(columns)


def test_missing_columns_accepts_increment_code_instead_of_time_control():
    columns = {"white_rating", "black_rating", "winner", "increment_code"}
    assert missing_columns(columns) == []


def test_missing_columns_reports_time_control_alternative():
    columns = {"white_rating", "black_rating", "winner"}
    assert "time_control or increment_code" in missing_columns(columns)


# ---- Нормалізація ----

def test_normalize_lichess_keeps_rated_only():
    df = pd.concat([frame(LICHESS_ROW), frame(LICHESS_ROW, id="b", rated="FALSE")])
    rows, profile, _ = normalize(df)
    assert profile == LICHESS
    assert len(rows) == MIN_ROWS


def test_normalize_lichess_derives_time_control_from_increment_code():
    rows, _, _ = normalize(frame(LICHESS_ROW))
    assert set(rows["time_control"]) == {"Rapid"}


def test_normalize_lichess_is_time_ordered():
    _, _, is_time_ordered = normalize(frame(LICHESS_ROW))
    assert is_time_ordered is True


def test_normalize_minimal_without_played_at_is_not_time_ordered():
    _, profile, is_time_ordered = normalize(frame(MINIMAL_ROW))
    assert profile == MINIMAL
    assert is_time_ordered is False


def test_normalize_minimal_with_played_at_is_time_ordered():
    _, _, is_time_ordered = normalize(frame(MINIMAL_ROW, played_at="2026-01-01"))
    assert is_time_ordered is True


def test_normalize_maps_winner_to_class():
    rows, _, _ = normalize(frame(MINIMAL_ROW))
    assert set(rows["y"]) == {0}


def test_normalize_accepts_winner_case_insensitively():
    rows, _, _ = normalize(frame(MINIMAL_ROW, winner="Black"))
    assert set(rows["y"]) == {2}


def test_normalize_rejects_missing_column():
    df = frame(MINIMAL_ROW).drop(columns=["winner"])
    with pytest.raises(ValueError, match="winner"):
        normalize(df)


def test_normalize_rejects_unknown_time_control():
    with pytest.raises(ValueError, match="контроль часу"):
        normalize(frame(MINIMAL_ROW, time_control="Hyperbullet"))


def test_normalize_rejects_unknown_winner():
    with pytest.raises(ValueError, match="winner"):
        normalize(frame(MINIMAL_ROW, winner="draw?"))


def test_normalize_drops_rows_with_unparseable_rating():
    df = pd.concat([frame(MINIMAL_ROW), frame(MINIMAL_ROW, white_rating="n/a", n=5)])
    rows, _, _ = normalize(df)
    assert len(rows) == MIN_ROWS


def test_normalize_rejects_too_few_rows():
    with pytest.raises(ValueError, match="замало рядків"):
        normalize(frame(MINIMAL_ROW, n=10))


# ---- Фільтри якості йдуть за колонками, а не за профілем ----

def test_normalize_minimal_with_rated_drops_unrated():
    """Та сама вибірка Lichess ходить по Kaggle у редакції без id і created_at,
    тож профіль у неї Minimal — але rated і turns у ній є. Прив'язка фільтрів
    до назви профілю тихо пускала нерейтингові партії в навчання.
    """
    good = frame(MINIMAL_ROW, rated="TRUE")
    bad = frame(MINIMAL_ROW, n=50, rated="FALSE")
    rows, profile, _ = normalize(pd.concat([good, bad]))

    assert profile == MINIMAL
    assert len(rows) == MIN_ROWS


def test_normalize_minimal_with_turns_drops_one_move_games():
    good = frame(MINIMAL_ROW, turns=20)
    bad = frame(MINIMAL_ROW, n=50, turns=1)
    rows, _, _ = normalize(pd.concat([good, bad]))

    assert len(rows) == MIN_ROWS


def test_normalize_minimal_without_rated_or_turns_keeps_everything():
    rows, _, _ = normalize(frame(MINIMAL_ROW, n=250))

    assert len(rows) == 250


def test_normalize_minimal_with_id_dedupes():
    duplicated = pd.concat([frame(MINIMAL_ROW), frame(MINIMAL_ROW)])
    duplicated["id"] = list(range(MIN_ROWS)) * 2
    rows, _, _ = normalize(duplicated)

    assert len(rows) == MIN_ROWS


# ---- Підсумок ----

def test_summarize_counts_by_time_control():
    rows, profile, ordered = normalize(frame(MINIMAL_ROW))
    summary = summarize(rows, profile, ordered)
    assert summary["profile"] == MINIMAL
    assert summary["rows"] == MIN_ROWS
    assert summary["by_time_control"]["Rapid"] == MIN_ROWS
    assert summary["by_time_control"]["Bullet"] == 0
    assert summary["is_time_ordered"] is False
    assert summary["issues"] == []


# ---- Контракт --validate ----

from pathlib import Path

from train import load, load_dataset, validate


def test_validate_reports_issue_for_missing_column(tmp_path: Path):
    path = tmp_path / "bad.csv"
    frame(MINIMAL_ROW).drop(columns=["winner"]).to_csv(path, index=False)
    verdict = validate(path)
    assert verdict["issues"]
    assert "winner" in verdict["issues"][0]
    assert verdict["rows"] == 0


def test_validate_reports_issue_for_empty_file(tmp_path: Path):
    path = tmp_path / "empty.csv"
    path.write_text("", encoding="utf-8")
    assert validate(path)["issues"]


def test_validate_accepts_minimal_file(tmp_path: Path):
    path = tmp_path / "good.csv"
    frame(MINIMAL_ROW).to_csv(path, index=False)
    verdict = validate(path)
    assert verdict["issues"] == []
    assert verdict["profile"] == MINIMAL
    assert verdict["rows"] == MIN_ROWS
    assert verdict["is_time_ordered"] is False


def test_load_still_returns_a_dataframe(tmp_path: Path):
    """build_notebook.py робить `df = train.load(...)` і далі `split(df)`.

    Якщо load почне віддавати кортеж, генератор chess_prediction.ipynb — тобто
    самого тексту курсової — тихо зламається. Кортеж має окрему назву.
    """
    path = tmp_path / "good.csv"
    frame(MINIMAL_ROW).to_csv(path, index=False)
    assert isinstance(load(path), pd.DataFrame)


def test_load_dataset_returns_frame_profile_and_ordering(tmp_path: Path):
    path = tmp_path / "good.csv"
    frame(MINIMAL_ROW).to_csv(path, index=False)
    rows, profile, is_time_ordered = load_dataset(path)
    assert isinstance(rows, pd.DataFrame)
    assert profile == MINIMAL
    assert is_time_ordered is False


# ---- CLI як його бачить бекенд (підпроцес, а не import) ----

import json
import subprocess
import sys


def run_cli(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run([sys.executable, "train.py", *args],
                          cwd=str(Path(__file__).parent),
                          capture_output=True, text=True, encoding="utf-8")


def test_cli_validate_prints_ukrainian_issue_without_crashing(tmp_path: Path):
    """Повідомлення про проблему українське, а stdout дочірнього процесу на
    Windows отримує cp1252 — друк кирилиці падав з UnicodeEncodeError, і
    бекенд бачив код 1 замість вердикту. Саме це тут і перевіряється.
    """
    path = tmp_path / "no-winner.csv"
    frame(MINIMAL_ROW).drop(columns=["winner"]).to_csv(path, index=False)

    result = run_cli("--validate", "--data", str(path))

    assert result.returncode == 0, result.stderr
    verdict = json.loads(result.stdout)
    assert "winner" in verdict["issues"][0]


def test_cli_validate_exits_zero_on_a_good_file(tmp_path: Path):
    path = tmp_path / "good.csv"
    frame(MINIMAL_ROW).to_csv(path, index=False)

    result = run_cli("--validate", "--data", str(path))

    assert result.returncode == 0, result.stderr
    assert json.loads(result.stdout)["issues"] == []
