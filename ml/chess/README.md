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

## Analysis notebook

```bash
.venv/Scripts/jupyter notebook chess_prediction.ipynb
```

The notebook (Ukrainian) is the coursework write-up: data, features, both
methods, comparison, calibration, conclusions. It imports `features.py` and
`train.py` rather than redefining anything.
