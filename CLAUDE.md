# T-Forge — esports tournament manager

Coursework project. ASP.NET Core 8 + EF Core 9 (PostgreSQL) API with a React 18 + TypeScript + Vite + Tailwind frontend. UI copy is Ukrainian.

## Layout

| Path | What |
|---|---|
| `Esport-Backend/` | API. Project file is `T-Forge.csproj`, namespace `TForge` (folder and project names differ — this is intentional) |
| `Esport-Backend.Tests/` | xUnit, `T-Forge.Tests.csproj`, namespace `TForge.Tests` |
| `Esport-Frontend/` | Vite app |
| `T-Forge.sln` | Solution at repo root |
| `docs/superpowers/` | Design specs and implementation plans |

Backend layers: `Controllers → Services → Data/Repositories (+ UnitOfWork) → EsportsDbContext`. DTOs are mapped with AutoMapper (`Mappings/MappingProfile.cs`), validation is FluentValidation, errors go through `Middleware/ExceptionMiddleware.cs`.

## Running it

```bash
# terminal 1
cd Esport-Backend && dotnet run          # http://localhost:5274, Swagger at /swagger

# terminal 2
cd Esport-Frontend && npm run dev        # http://localhost:5173
```

Ports are not arbitrary: `Esport-Frontend/.env` points at `:5274`, and backend CORS allows only `:5173`. Changing either breaks the connection and SignalR.

PostgreSQL is expected at `localhost:5432`, user `postgres`, password `1111`, database `EsportsDB`. Migrations and seeding run automatically at startup (`Data/DatabaseInitializer.cs`).

**Seeded accounts:** `admin`, `organizer1`, `player1`–`player4`, all with password `DevPassw0rd` (`DbSeeder.DevPassword`).

## Verifying changes

```bash
dotnet build T-Forge.sln                                # must be 0 warnings, 0 errors
dotnet test Esport-Backend.Tests/T-Forge.Tests.csproj    # 19 tests
cd Esport-Frontend && npx tsc --noEmit && npx vite build
```

There is no browser automation available in this environment. Verify UI behaviour through the API with `curl`, and say so plainly rather than implying a visual check. When behaviour depends on generated SQL, read the SQL — `appsettings.Development.json` already logs `Microsoft.EntityFrameworkCore.Database.Command` at Information.

**Never test against `EsportsDB`.** Use a scratch database and drop it afterwards:

```bash
cd Esport-Backend
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=TForge_Scratch;Username=postgres;Password=1111"
export ASPNETCORE_URLS="http://localhost:5301" ASPNETCORE_ENVIRONMENT=Development
dotnet run --no-launch-profile
# …then:
dotnet ef database drop -f
```

`psql` is not on PATH.

## Conventions that matter

- **Never write status strings as literals.** Use `TForge.Common.MatchStatus`, `TournamentStatus`, `MatchTypes`, `ResultType`. Frontend and backend must agree on the same values — a past bug had four layers disagreeing (`"InProgress"` vs `"In Progress"` vs `"Active"`), which silently broke live matches and the dashboard.
- **Player statistics have one source: `MatchPlayer` rows.** `MatchPlayer.TeamId` records the team a player represented *in that match*, stamped at creation and never recalculated, so transfers don't rewrite history. Never decide a result from `Player.TeamId` (current team). `Player.TotalMatches/Wins/Losses/WinRate` is a denormalised cache maintained by `MatchRosterService.ApplyMatchResultAsync`.
- **Statistics arithmetic lives in pure calculators** — `Common/TeamRecordCalculator.cs`, `Common/PlayerRecordCalculator.cs`. No EF, no services. That is what makes them unit-testable; keep new arithmetic there.
- **Read endpoints for standings/summaries/profiles are public** (no `[Authorize]`). Writes are role-gated; ownership checks use `Controllers/ApiControllerBase.cs` (`ResolveOwnerId`, `IsAdmin`).
- **Owner ids are server-derived.** Create DTOs take a nullable `CaptainId`/`OrganizerId`/`UserId`; only an Admin may pass one explicitly. Forms must never ask a user to type an id.
- **Paged queries must have a deterministic order.** `Extensions/QueryableExtensions.cs` `ApplyPaging` inspects the expression tree for `OrderBy`/`ThenBy` and falls back to `Id`. Do not "simplify" that to an `IOrderedQueryable<T>` type test — EF Core's `EntityQueryable<T>` implements that interface unconditionally, so the check silently never fires.
- **A missing entity must 404.** `return Ok(null)` serialises as HTTP 204, which clients read as success. Use `?? throw new EntityNotFoundException("Thing", id)`.
- **Frontend styling uses the existing system**: classes `panel`, `table`, `btn`, `pill`, `eyebrow`, `tabular`, `surface-raised`; palette tokens `ink-*`, `line`, `ember`, `win`, `danger`, `text-*`. `ember` is the single accent — spend it on primary actions and live state only. Fonts: Unbounded (display, sparing), IBM Plex Sans (UI), IBM Plex Mono (data).
- **Paginated lists use `hooks/usePagedList.ts`.** Its `resetKey` must contain every value the fetch closure captures (search term, entity id). Get that wrong and the list silently stops refreshing.

## Current state

Branch `feature/team-player-history` adds team match history, player profiles, and list pagination. Two commits exist; the rest is **staged but uncommitted** — the repo owner commits manually. `docs/` is untracked.

## Known issues, roughly by value

1. **Security, untouched.** Passwords are SHA-256 with one shared salt (`Services/PasswordHasher.cs` — deliberately isolated so swapping in BCrypt is a one-file change). Public registration accepts `Role: "Admin"`. JWT signing key and DB password are committed in `appsettings.json`. No refresh tokens; logout is a no-op. Organizers can edit *any* tournament — `OrganizerId` is never compared to the caller.
2. **Counter drift.** Editing a match roster after completion, or re-completing a match via `PUT` (`MatchService.UpdateAsync` maps `Status`/`WinnerTeamId` straight through), desynchronises the cached `Player.*` counters from the derived statistics. Needs status guards on those endpoints.
3. **`PUT /api/players/{id}` silently clears team membership** when `teamId` is omitted — `UpdatePlayerDto.TeamId` is nullable and mapped unconditionally.
4. **Pagination has never been exercised in a browser.** Worth one manual pass on a list with 20+ rows.
5. **Shadow FK columns** `matches.TeamId1` and `players.UserId1` exist because `Team.HomeMatches`/`AwayMatches` and `User.PlayerProfile` aren't wired into the configured relationships. EF warns on every command. Fixing needs a migration.
6. **Test coverage is 19 unit tests over the pure calculators only.** No integration or frontend tests. No fractional-KDA case pins the 2-decimal rounding.

## Suggested next work

Security (item 1) is the highest value and splits cleanly: BCrypt via the existing `IPasswordHasher`; restrict registration roles; move secrets to user-secrets/env; add tournament ownership checks. Item 2 is a small, well-understood follow-up. Beyond fixes, the unbuilt features worth considering are double-elimination or group-stage brackets, and team logos (there is no image handling anywhere yet).

Design specs and implementation plans for completed work live in `docs/superpowers/` and are worth reading before extending those areas.
