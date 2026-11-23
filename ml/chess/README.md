# Chess result prediction — ML study and model export

Coursework part «Прогнозування результатів кіберспортивних змагань методами
машинного навчання», applied to chess. Two methods are trained and compared
against an Elo baseline:

1. **Logistic Regression** (multinomial, standardised features)
2. **Gradient Boosted Trees** (`HistGradientBoostingClassifier`)

Target: White win / Draw / Black win. Features are only what T-Forge knows
before a game: rating difference, mean rating, time control. See
`docs/superpowers/specs/2026-11-05-chess-discipline-and-prediction-design.md`.

## Data

Kaggle «Chess Game Dataset (Lichess)» (`datasnaek/chess`), file `games.csv`
(~20k games). Kaggle needs a login, so download it by hand to:

```
ml/chess/data/raw/games.csv
```

`data/raw/` is gitignored. Note: the copy used for the coursework had passed
through a spreadsheet, so part of `created_at` is rounded to 6 significant
digits (≈ ±3 hours). The time-ordered split is still meaningful — the games
span several years.

## Setup

```bash
cd ml/chess
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt
.venv/Scripts/python -m pytest -q
```

## Train and export

```bash
.venv/Scripts/python train.py
```

This trains both models, prints the test metrics, and **copies its outputs
into the app**:

- `Esport-Backend/MlModels/chess/{logreg.onnx, gbt.onnx, model_info.json}` — loaded by `ChessModelRunner`
- `Esport-Backend.Tests/Fixtures/chess_parity.json` — checked by `ChessModelParityTests`

After retraining, run `dotnet test` — the parity test must still pass.

Flags:

- `--data <csv>` — train on another file (see **Dataset profiles** below)
- `--out <dir>` — write artifacts somewhere other than `out/`
- `--no-install` — do **not** copy the result into `Esport-Backend/MlModels/chess`
  and `Esport-Backend.Tests/Fixtures`. The app always passes this, which is what
  makes it impossible for UI-driven training to overwrite the committed baseline
  or the parity fixture
- `--validate` — read the file, print a JSON verdict, train nothing. Exits 0
  whenever it produced a verdict: an unusable file is a non-empty `issues[]`, not
  a non-zero exit, so a broken venv stays distinguishable from a bad CSV

## Dataset profiles

`datasets.py` is the only definition of what a training CSV may look like. The
backend never parses one itself — it runs `train.py --validate` and reads the
verdict, so the schema has a single definition.

- **Lichess** — all of `id`, `rated`, `turns`, `created_at`, `winner`,
  `increment_code`, `white_rating`, `black_rating`. Deduped on `id`, rated games
  only, `turns >= 2`, time control derived from `increment_code`, ordered by
  `created_at`.
- **Minimal** — `white_rating`, `black_rating`, `winner`, and one of
  `time_control` (a `Common/TimeControls.cs` value) or `increment_code`.
  Optional `played_at` for chronological ordering; without it the train/test
  split is positional and `model_info.json` records `is_time_ordered: false`.

`winner` is `white` / `draw` / `black`, case-insensitive. Minimum 200 usable rows.

`load(path)` still returns just the DataFrame, because `build_notebook.py` calls
it that way; `load_dataset(path)` returns `(rows, profile, is_time_ordered)`.

## Analysis notebook

```bash
.venv/Scripts/jupyter notebook chess_prediction.ipynb
```

The notebook (Ukrainian) is the coursework write-up: data, features, both
methods, comparison, calibration, conclusions. It imports `features.py` and
`train.py` rather than redefining anything.
