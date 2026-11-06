# T-Forge

An esports tournament manager built as a coursework project. Organizers run
tournaments with brackets, captains manage teams and challenge each other to
friendly matches, players find teams through an LFT feed, and every result
feeds an Elo ladder. Five disciplines are supported: **CS2, Valorant, Dota 2,
League of Legends and Chess**.

For chess, T-Forge also **predicts results with machine learning**: two models
(Logistic Regression and Gradient Boosting) trained on Lichess games show
win / draw / loss probabilities on duels and team matches, next to an Elo
baseline.

The UI is in Ukrainian.

## Features

| Area | What it does |
|---|---|
| **Accounts and roles** | Registration as a Player; the Organizer role is requested and granted by an Admin. Profiles with avatars, country flags and per-field privacy. |
| **Tournaments** | Create, edit, register or invite teams (open or invite-only), single-elimination bracket generation. |
| **Matches** | Scheduling, live scores over SignalR, rosters filled automatically, stream and tracker links. |
| **Friendly matches** | Captains challenge other teams; accepting creates a match outside any tournament. |
| **Duels (1 vs 1)** | Players challenge each other directly, or post an open challenge anyone can accept. |
| **Map / hero draft** | A live ban/pick screen for CS2, Valorant (maps) and Dota 2, LoL (heroes), with a server-side turn timer. |
| **LFT matchmaking** | Free agents and captains swipe on each other; filters on discipline, role, rank and timezone. |
| **Elo ladder** | Rating per team and per player in each discipline, an append-only rating history, tier badges. |
| **Notifications** | Invitations, membership requests, team challenges and duel challenges addressed to the right person. |
| **Chess** | Team chess on four boards and individual chess duels, with time controls and rating bands. |
| **Result prediction** | ML probabilities for chess duels and team matches (see below). |

## Tech stack

- **Backend:** ASP.NET Core 8, Entity Framework Core 9, PostgreSQL, SignalR,
  AutoMapper, FluentValidation, JWT auth, ONNX Runtime
- **Frontend:** React 18, TypeScript, Vite, Tailwind CSS, react-hook-form + zod,
  zustand
- **ML:** Python, pandas, scikit-learn, skl2onnx, Jupyter
- **Tests:** xUnit (backend), pytest (ML features)

## Repository layout

```
T-Forge/
├─ Esport-Backend/          ASP.NET Core API (project T-Forge.csproj, namespace TForge)
│  ├─ Controllers/          HTTP endpoints
│  ├─ Services/             business logic
│  ├─ Common/               pure rules and calculators (policies, Elo, catalogs, chess features)
│  ├─ Data/                 DbContext, repositories, migrations runner, seed data
│  └─ MlModels/chess/       trained ONNX models loaded at startup
├─ Esport-Backend.Tests/    xUnit tests, including the ML parity test
├─ Esport-Frontend/         React + Vite app
├─ ml/chess/                training pipeline and analysis notebook
├─ tools/                   scripts that generate the hero catalog and fetch map images
├─ docs/                    design specs and implementation plans (not tracked by git)
└─ T-Forge.sln
```

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 18 or newer
- PostgreSQL running on `localhost:5432`
- Python 3.12+ (only to retrain the models)

### 1. Configure secrets

Secrets are not stored in the repository. Set them once per machine:

```bash
cd Esport-Backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=EsportsDB;Username=postgres;Password=<your password>"
dotnet user-secrets set "Jwt:SecretKey" "<any string of 32+ characters>"
```

The backend refuses to start with a clear message if either value is missing.

### 2. Run the backend

```bash
cd Esport-Backend
dotnet run
```

The API listens on **http://localhost:5274**, with Swagger at `/swagger`. On
startup it applies database migrations and seeds demo data automatically.

### 3. Run the frontend

```bash
cd Esport-Frontend
npm install
npm run dev
```

Open **http://localhost:5173**.

> The ports are fixed: `Esport-Frontend/.env` points at `:5274`, and the
> backend's CORS policy allows only `:5173`. Changing either breaks the API
> calls and live updates.

### Demo accounts

All seeded accounts use the password `DevPassw0rd`.

| Username | Role |
|---|---|
| `admin` | Admin, including a developer mode to preview other roles |
| `organizer1` | Organizer |
| `player1` … `player4` | Players on the seeded esports teams |
| `chess1` … `chess8` | Chess players on two teams, with a chess tournament and duels |

## Chess result prediction

```
Lichess games ──► features.py ──► train.py ──► logreg.onnx, gbt.onnx
                                                     │
                                                     ▼
PredictionPanel.tsx ◄── /api/…/prediction ◄── ChessModelRunner (ONNX Runtime)
```

- **Task:** for two players, predict the probability of a White win, a draw
  and a Black win before the game starts.
- **Data:** about 15,000 rated games from the Kaggle "Chess Game Dataset (Lichess)".
- **Features:** only what is known in advance: the rating difference, the
  mean rating and the time control.
- **Methods:** Logistic Regression and Gradient Boosting, compared against
  the Elo formula. The data is split by date: older games for training,
  newer ones for testing.

Results on 3,093 test games:

| Method | Log loss ↓ | Brier ↓ | Accuracy |
|---|---|---|---|
| Logistic Regression | **0.786** | **0.485** | **61.0 %** |
| Gradient Boosting | 0.789 | 0.487 | 60.6 % |
| Elo baseline | 0.813 | 0.485 | 60.5 % |

Both models beat Elo on log loss, the main metric because the app shows
probabilities. The dataset has almost no bullet or blitz games, so
predictions for those time controls are marked «мало даних» in the app.

In a duel, the server stamps who plays White by coin flip when the duel is
accepted. In a team match, players are paired by board number, and the panel
shows expected points per team.

The full analysis, with charts, is in
[`ml/chess/chess_prediction.ipynb`](ml/chess/chess_prediction.ipynb).

### Retraining

```bash
cd ml/chess
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt   # on Linux/macOS: .venv/bin/python
.venv/Scripts/python train.py                             # or: train.py --data <another Lichess CSV>
```

`train.py` copies the new models into `Esport-Backend/MlModels/chess/` and
refreshes the parity fixture. Then run `dotnet test`: the parity test checks
that the C# feature code and the Python feature code still give the same
probabilities. The dataset must be downloaded by hand from Kaggle; see
[`ml/chess/README.md`](ml/chess/README.md).

## Testing

```bash
dotnet build T-Forge.sln                                # expected: 0 warnings, 0 errors
dotnet test Esport-Backend.Tests/T-Forge.Tests.csproj    # 828 tests
cd Esport-Frontend && npx tsc --noEmit && npx vite build
cd ml/chess && .venv/Scripts/python -m pytest -q          # 14 tests
```

Tests cover the pure rules: policies, calculators, catalogs, validators,
and the ML features and models. There are no integration or browser tests;
services that touch the database are checked by hand against a scratch
database.

## Known limitations

- No refresh tokens: logout does not revoke a token, which stays valid until
  it expires.
- Brackets are single-elimination only.
- Chess predictions rely on ratings the players enter themselves, and the
  training data covers mostly rapid and classical games.

## Further documentation

- [`CLAUDE.md`](CLAUDE.md): architecture, conventions and the reasons behind
  them, for anyone changing the code.
- `docs/superpowers/specs/`: a design spec for each major feature (kept
  locally, not in git).
