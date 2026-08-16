# T-Forge — esports tournament manager

Coursework project. ASP.NET Core 8 + EF Core 9 (PostgreSQL) API with a React 18 + TypeScript + Vite + Tailwind frontend. UI copy is Ukrainian.
Don't mark yourself as co-author and do not commit any changes by yourself
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
dotnet test Esport-Backend.Tests/T-Forge.Tests.csproj    # 186 test cases
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

- **Never write status, role, position or game strings as literals.** Use `TForge.Common.MatchStatus`, `TournamentStatus`, `MatchTypes`, `ResultType`, `UserRoles`, `PlayerPositions`, `Games`, `MatchChallengeStatus`.
- **`Match.TournamentId` is nullable, and null means a friendly match** created by accepting a `MatchChallenge` between two captains. Friendlies are stamped `Round = 0` and `MatchType = GroupStage`, which is what keeps `BracketService` away from them and keeps them out of the titles count (only `Final` wins count). They *do* contribute to team and player win/loss records and KDA — that is deliberate, they are real matches with real rosters. Any new query over matches must decide explicitly whether it means "all matches" or "tournament matches only".
- **`Match.Game` is server-derived**, copied from the tournament wherever a match is created — `MatchService.CreateAsync` and *both* `BracketService` paths. `MappingProfile` explicitly ignores it on `CreateMatchDto -> Match`, which is what stops a client-supplied value from ever being honoured; don't remove that `Ignore`. A match created with an empty game is invisible to the game filter. Frontend and backend must agree on the same values — a past bug had four layers disagreeing (`"InProgress"` vs `"In Progress"` vs `"Active"`), which silently broke live matches and the dashboard.
- **Player statistics have one source: `MatchPlayer` rows.** `MatchPlayer.TeamId` records the team a player represented *in that match*, stamped at creation and never recalculated, so transfers don't rewrite history. Never decide a result from `Player.TeamId` (current team). `Player.TotalMatches/Wins/Losses/WinRate` is a denormalised cache maintained by `MatchRosterService.ApplyMatchResultAsync`.
- **Statistics arithmetic lives in pure calculators** — `Common/TeamRecordCalculator.cs`, `Common/PlayerRecordCalculator.cs`. No EF, no services. That is what makes them unit-testable; keep new arithmetic there.
- **Read endpoints for standings/summaries/profiles are public** (no `[Authorize]`). Writes are role-gated; ownership checks use `Controllers/ApiControllerBase.cs` (`ResolveOwnerId`, `IsAdmin`).
- **Owner ids are server-derived.** Create DTOs take a nullable `CaptainId`/`OrganizerId`/`UserId`; only an Admin may pass one explicitly. Forms must never ask a user to type an id.
- **Paged queries must have a deterministic order.** `Extensions/QueryableExtensions.cs` `ApplyPaging` inspects the expression tree for `OrderBy`/`ThenBy` and falls back to `Id`. Do not "simplify" that to an `IOrderedQueryable<T>` type test — EF Core's `EntityQueryable<T>` implements that interface unconditionally, so the check silently never fires.
- **A missing entity must 404.** `return Ok(null)` serialises as HTTP 204, which clients read as success. Use `?? throw new EntityNotFoundException("Thing", id)`.
- **Frontend styling uses the existing system**: classes `panel`, `table`, `btn`, `pill`, `eyebrow`, `tabular`, `surface-raised`; palette tokens `ink-*`, `line`, `ember`, `win`, `danger`, `text-*`. `ember` is the single accent — spend it on primary actions and live state only. Fonts: Unbounded (display, sparing), IBM Plex Sans (UI), IBM Plex Mono (data).
- **Player field rules live in `Validators/PlayerRules.cs`** as FluentValidation extension methods (`PlayerNickname`, `PlayerPosition`, `PlayerCountry`, `PlayerAge`). Four validators used to carry their own copies and had drifted apart — nickname minimum 2 in one place and 3 in another, position whitelisted in one and free-text in another. Add new player-field rules there, and mirror any change in `Esport-Frontend/src/constants/playerPositions.ts`, which the forms read.
- **Role-gated UI reads `hooks/useEffectiveRole.ts`**, never `user.role` directly. Developer mode (Admin-only) substitutes a role client-side, so a component reading the raw role stays stuck in the real one and the mode works only halfway. Ownership checks are the exception: compare `user.id` to the real id, so previewing a role never grants rights over someone else's record. Guard: `grep -rn 'role === "' src/` should only hit `AppShell`'s `isRealAdmin` and form-value comparisons.
- **List statistics are derived in SQL, not read from caches.** `GET /api/players/paged` and `/api/teams/paged` return `PlayerRowDto`/`TeamRowDto` — deliberately *not* `PlayerDto`/`TeamDto`. Their win/loss/KDA/titles come from correlated subqueries over `MatchPlayer`/`HomeMatches`/`AwayMatches`, matching `PlayerRecordCalculator`'s definition, so a list never disagrees with a profile even when `Player.TotalMatches` and friends have drifted. `PlayerDto.TotalMatches/Wins/Losses/WinRate` remain the denormalised cache; don't mix the two.
- **Sorting is server-side and whitelisted.** `Common/PlayerSortKeys.cs` and `TeamSortKeys.cs` map a key to an order expression; an unknown key falls back to the default order rather than throwing. Mirrored in `Esport-Frontend/src/constants/sortKeys.ts` and driven by `components/ui/SortableTh.tsx` + `useSortState`. Sorting client-side would only reorder the visible page. `QueryableExtensions.ApplySorting` (reflection-based) is still used elsewhere but cannot reach nested or computed columns.
- **Every form must surface server errors.** Client validation only knows what the form knows; uniqueness, permissions and entity state are server-only. Use `hooks/useSubmitError.ts` and render `notice notice-error`. Guard: every file matching `src/features/*/*Form.tsx` plus the auth and profile pages should contain `notice-error`.
- **Avatars are files on disk, not rows in the database.** `User.AvatarPath` stores a root-relative path; the bytes live under `Esport-Backend/wwwroot/uploads/avatars/` (gitignored, so they don't travel with the repo). `Program.cs` creates that folder and sets `WebRootPath` **before** the host is built and passes an explicit `PhysicalFileProvider` to `UseStaticFiles` — without that, `WebRootPath` is null on a clean checkout and every uploaded avatar 404s. Upload type is decided by `Common/AvatarRules.cs` from the leading magic bytes, never from `Content-Type` or the filename, both of which the client controls.
- **Who may run a match is decided by `Common/FriendlyMatchPolicy.cs`** — a pure function, like the other authorization rules. `MatchesController` calls `EnsureCanManageAsync` on start/complete/score/stream instead of carrying a role attribute. `MatchDto.HomeTeamCaptainId`/`AwayTeamCaptainId` exist so the client can mirror that check: `TeamSummaryDto.Captain` is **not** loaded on match responses, so testing `team.captain?.id` there silently fails.
- **A match carries two optional external links, validated by different rules.** `Match.StreamUrl` is host-allow-listed in `Common/StreamUrlRules.cs` and compared by exact host match — never `Contains("twitch.tv")`, since `twitch.tv.evil.com` contains it. `Match.TrackerUrl` (the post-match stats page: tracker.gg, HLTV, Dotabuff, OP.GG) is deliberately **not** host-restricted, because every discipline has its own tracker; `Common/TrackerUrlRules.cs` requires https, a real host and ≤300 chars. Both are set through `PUT /api/matches/{id}/links`, and the tracker one can also be supplied when completing a match.
- **Paginated lists use `hooks/usePagedList.ts`.** Its `resetKey` must contain every value the fetch closure captures (search term, entity id). Get that wrong and the list silently stops refreshing.

## Current state

Branch `feature/scope-phase1-accounts-roles` implements Phases 1 and 2 of the `Scope.md` work: registration restricted to Player/Organizer with an auto-created player profile, an Admin-only full-account endpoint, a `/profile` page, Admin-only developer mode, and the game catalog with match filtering. **Nothing is committed** — the repo owner commits manually.

Phase 2 is also done: a four-title game catalog (`Common/Games.cs`), tournaments validated against it, `Match.Game` stamped from the tournament, and a game filter on the matches page.

Phase 3 is done too: captains challenge other teams to friendly matches (`MatchChallenge` + `MatchChallengePolicy`), and accepting creates a match with no tournament.

Phase 4 is done: the shadow foreign keys are repaired (**five** columns, not the two previously documented — `matches.TeamId`, `matches.TeamId1`, `players.UserId1`, `teams.UserId`, `tournaments.UserId`; EF model warnings are now zero), the Players and Teams lists carry sortable standings columns computed in SQL, `/standings` is gone, every form reports server errors, players can create teams, the auth pages have back buttons, and the matches page uses tabs.

Phase 5 is done: avatars (upload, serve, remove) and stream links, with the matches page subscribing to `MatchHub` so live scores tick without a refresh.

**All of `Scope.md` is now implemented.** The design is `docs/superpowers/specs/2026-08-16-scope-features-design.md`, with per-phase plans alongside it in `docs/superpowers/plans/`.

Captains now also run their own friendly matches: start, score, complete and the stream link. `Common/FriendlyMatchPolicy.cs` decides who may — organizers and admins keep their rights over every match, while a *friendly* may additionally be run by either team's captain. A captain still cannot touch a tournament match, which would let them award themselves a title.

## Known issues, roughly by value

1. **Security, mostly untouched.** Passwords are SHA-256 with one shared salt (`Services/PasswordHasher.cs` — deliberately isolated so swapping in BCrypt is a one-file change). JWT signing key and DB password are committed in `appsettings.json`. No refresh tokens; logout is a no-op. Organizers can edit *any* tournament — `OrganizerId` is never compared to the caller. *Fixed:* public registration no longer accepts `Role: "Admin"` — `RegisterValidator` restricts it to `UserRoles.SelfService`.
2. **Counter drift.** Editing a match roster after completion, or re-completing a match via `PUT` (`MatchService.UpdateAsync` maps `Status`/`WinnerTeamId` straight through), desynchronises the cached `Player.*` counters from the derived statistics. Needs status guards on those endpoints.
3. **Pagination has never been exercised in a browser.** Worth one manual pass on a list with 20+ rows.
4. **Test coverage is 186 test cases over pure calculators, policies, constants and validators.** No integration or frontend tests — services that touch EF are verified by hand against a scratch database. No fractional-KDA case pins the 2-decimal rounding.

## Suggested next work

Security (item 1) is the highest value and splits cleanly: BCrypt via the existing `IPasswordHasher`; move secrets to user-secrets/env; add tournament ownership checks. (Registration roles are already restricted.) Item 2 is a small, well-understood follow-up. Beyond fixes, the unbuilt features worth considering are double-elimination or group-stage brackets, and team logos (there is no image handling anywhere yet).

Design specs and implementation plans for completed work live in `docs/superpowers/` and are worth reading before extending those areas.
