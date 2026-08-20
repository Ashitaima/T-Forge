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

**Secrets are not in the repository.** `appsettings.json` ships with empty
placeholders for `ConnectionStrings:DefaultConnection` and `Jwt:SecretKey`;
startup fails with an explicit message if either is missing. Set them once per
machine:

```bash
cd Esport-Backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=EsportsDB;Username=postgres;Password=1111"
dotnet user-secrets set "Jwt:SecretKey" "<any string of 32+ characters>"
```

Environment variables (`ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`)
override user-secrets, which is what makes the scratch-database recipe below work
unchanged.

**Seeded accounts:** `admin`, `organizer1`, `player1`–`player4`, all with password `DevPassw0rd` (`DbSeeder.DevPassword`).

## Verifying changes

```bash
dotnet build T-Forge.sln                                # must be 0 warnings, 0 errors
dotnet test Esport-Backend.Tests/T-Forge.Tests.csproj    # 301 test cases
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
- **Image uploads go through `Services/ImageUploadService.cs`.** It knows a folder
  and a subject id, nothing else; `AvatarService` (`User.AvatarPath`) and
  `TeamLogoService` (`Team.LogoPath`) are thin callers. The rule the split exists
  to protect is that the previous file is deleted only *after* the row saves —
  deleting first and then failing the save leaves the subject with no image at
  all. Type is decided by `Common/AvatarRules.cs` from the magic bytes, never from
  `Content-Type` or the filename. `LogoPath` is absent from `CreateTeamDto`/
  `UpdateTeamDto` on purpose, and `Esport-Backend.Tests/TeamDtoSurfaceTests.cs`
  pins that. `TeamRowDto` is hand-projected in `TeamService.GetPagedRowsAsync`, so
  a new column has to be added there too or it is null in the list only.
- **Notifications have no table.** `Services/NotificationService.cs` projects
  `TeamMembershipRequest`, `MatchChallenge` and `TournamentInvitation` rows into
  one DTO, and `Common/NotificationAddressing.cs` — pure, like the other policies —
  decides who each row is for: while it is pending it awaits the responder, once
  answered it informs the initiator, and a cancelled row addresses nobody.
  `RespondedByUserId` is never the audience. Unread is one column,
  `User.NotificationsSeenAt`, not a flag per row, and `POST /api/notifications/seen`
  is the only thing that writes it. `GetUnreadCountAsync` deliberately reuses the
  same collection instead of a leaner SQL `COUNT`: the addressing rule is C# and
  EF cannot translate it, so counting in SQL would mean a second copy of the one
  rule that must not drift. The ceiling is real — only those three flows can
  produce a notification, so anything else needs an events table.

- **Rating arithmetic lives in `Common/EloCalculator.cs`**, alongside the record calculators — pure, no EF. `Services/RatingService.cs` only reads and writes rows. Rating is per `(subject, game)`; only tournament matches with a winner count, so a friendly never moves it. The `TeamRatingChanges`/`PlayerRatingChanges` ledger is what makes double-counting structurally impossible: the service checks for an existing row before rating, and the unique `(TeamId, MatchId)` index catches anything that slips past. Rating hooks into `MatchRosterService.ApplyMatchResultAsync` *after* the roster is materialised — players are paid from `MatchPlayer.TeamId`, so those rows must exist first.
- **Country is an ISO 3166-1 alpha-2 code, not a name.** `Common/Countries.cs` holds the codes; the Ukrainian labels and the flag live in `Esport-Frontend/src/constants/countries.ts`, same split as `Games`. `PlayerRules.PlayerCountry` whitelists them — free text can't produce a flag. `DatabaseInitializer.NormalizeLegacyCountriesAsync` translates known old names once and leaves unknown ones alone rather than wiping them. Flags are emoji: Windows lacks the glyphs and renders the two letters instead, which is why the code or name always sits next to the flag.
- **Tournament writes check ownership through `Common/TournamentOwnershipPolicy.cs`** —
  a pure function, like `FriendlyMatchPolicy`. The `Organizer` role grants the right
  to create tournaments, never the right to edit someone else's, so `UpdateAsync` and
  `GenerateAsync` both take the caller's id and `isAdmin`. `[Authorize(Roles = "…")]`
  alone is not an ownership check — that was the bug. `Common/TeamCaptaincyPolicy.cs`
  is the same shape for `Team.CaptainId`, and `PUT /api/teams/{id}/captain` is the
  only way that column changes.
- **Admin is above every ownership rule.** Role attributes read `"Admin,Organizer"`,
  never `"Organizer"` alone, and hand-written owner checks in controllers are guarded
  with `!IsAdmin &&`. An admin who can delete a tournament but not fix a typo in it
  was the symptom; grep for `Roles = "` and for `Captain?.Id !=` when adding either.
- **Password hashes carry their own algorithm marker.** `PasswordHasher.Verify`
  dispatches on the `$2` prefix, so BCrypt and the legacy SHA-256 hashes coexist;
  `NeedsRehash` plus the upgrade in `AuthService.LoginAsync` is what drains the old
  ones. Never compare hashes outside this class.
- **A corrected result is reversed, never rewritten.** The rating ledger is
  append-only, so `RatingService.RateMatchAsync` compares
  `TeamRatingChange.RecordedWinnerTeamId` on the highest-`Revision` row against the
  match's current winner and, when they differ, writes a `Reversal` pair and then a
  fresh `Applied` pair at the next revisions (`Common/RatingChangeKinds.cs`). The
  unique index is `(TeamId, MatchId, Revision)` — extended, not dropped, so
  double-counting is still structurally impossible. `MatchService.UpdateAsync` is the
  path that makes this necessary: it maps `Status`/`WinnerTeamId` straight through.
- **Cached player counters are recomputed, not incremented.**
  `MatchRosterService.ApplyMatchResultAsync` rebuilds `Player.TotalMatches/Wins/
  Losses/WinRate` from `MatchPlayer` rows via `PlayerRecordCalculator` — the same
  calculator the profile and the lists read. Incrementing was correct exactly once
  and drifted on every roster edit or re-`PUT`; recomputing makes the method
  idempotent, so there is nothing left to drift from.
- **Date and time are picked with `components/ui/DateTimePicker.tsx`**, never `input[type=datetime-local]` — its segment-by-segment mask was the complaint that prompted the change. `mode="date"` drops the time half. The emitted string matches the native control's, so form wiring is unchanged, and the text field still accepts typing.

## Current state

Branch `feature/scope-phase1-accounts-roles` implements Phases 1 and 2 of the `Scope.md` work: registration restricted to Player/Organizer with an auto-created player profile, an Admin-only full-account endpoint, a `/profile` page, Admin-only developer mode, and the game catalog with match filtering. **Nothing is committed** — the repo owner commits manually.

Phase 2 is also done: a four-title game catalog (`Common/Games.cs`), tournaments validated against it, `Match.Game` stamped from the tournament, and a game filter on the matches page.

Phase 3 is done too: captains challenge other teams to friendly matches (`MatchChallenge` + `MatchChallengePolicy`), and accepting creates a match with no tournament.

Phase 4 is done: the shadow foreign keys are repaired (**five** columns, not the two previously documented — `matches.TeamId`, `matches.TeamId1`, `players.UserId1`, `teams.UserId`, `tournaments.UserId`; EF model warnings are now zero), the Players and Teams lists carry sortable standings columns computed in SQL, `/standings` is gone, every form reports server errors, players can create teams, the auth pages have back buttons, and the matches page uses tabs.

Phase 5 is done: avatars (upload, serve, remove) and stream links, with the matches page subscribing to `MatchHub` so live scores tick without a refresh.

**All of `Scope.md` is now implemented** — including the second round of eight items (2026-08-17):

1. Developer mode gained a team picker: an Admin can view the app as a given team's captain. Captaincy is `Team.CaptainId`, not a role, so the role dropdown could not express it. Read it through `hooks/useEffectiveRole.ts` → `useIsCaptainOf(teamId, teamCaptainId)`.
2. The matches page splits on two axes: **Турніри / Практичні** as tabs (the real distinction — friendlies give no titles and no rating), with Заплановані / Зіграні as a switch inside each.
3. Date and time are picked with `DateTimePicker`, not the native mask.
4. Player countries are ISO codes with flags.
5. Rosters auto-fill the moment a match is created — all four paths (`MatchService.CreateAsync`, both `BracketService` paths, `MatchChallengeService.AcceptAsync`). The manual button stays for topping up after transfers.
6. Tournaments have `IsInviteOnly`. `TournamentInvitation` + `Common/TournamentInvitationPolicy.cs` mirror the membership-request pattern: organizers invite, captains apply, and `CanRegisterDirectly` is what closes self-registration on a closed tournament.
7. Status is gone from tournament creation — the server always starts one at `Registration`. The field appears only when editing.
8. An empty discipline now reads «Оберіть дисципліну». The fix is a zod `errorMap`, not `required_error`: an empty `<select>` is an invalid enum value, not a missing field, so `required_error` never fired.

Plus the ranked ladder from `docs/superpowers/specs/2026-08-16-elo-ranked-ladder-design.md`: Elo per team and player per discipline, an append-only ledger, tier badges, sortable rating columns in both lists, rating panels with a sparkline on both profiles, and a per-match delta on the match page. Backfill replays completed tournament matches on startup through the production calculator.

Captains now also run their own friendly matches: start, score, complete and the stream link. `Common/FriendlyMatchPolicy.cs` decides who may — organizers and admins keep their rights over every match, while a *friendly* may additionally be run by either team's captain. A captain still cannot touch a tournament match, which would let them award themselves a title.

## Known issues, roughly by value

1. **Security.** *Fixed:* passwords are BCrypt with a per-password salt and work
   factor 12 (`Services/PasswordHasher.cs` still verifies the old SHA-256 hashes
   and upgrades them on next login — delete that path once no legacy hash
   remains); the JWT key and DB password live in user-secrets; tournament writes
   check `OrganizerId` through `Common/TournamentOwnershipPolicy.cs`; login and
   registration are rate-limited to 10 requests per minute per IP; public
   registration no longer accepts `Role: "Admin"`. *Still open:* no refresh
   tokens, so logout is a no-op and a stolen token is valid until it expires.
   Note that the old JWT key and DB password remain in git history.
2. **Pagination has never been exercised in a browser.** Worth one manual pass on a list with 20+ rows.
3. **Test coverage is 301 test cases over pure calculators, policies, constants and validators.** No integration or frontend tests — services that touch EF are verified by hand against a scratch database. No fractional-KDA case pins the 2-decimal rounding.
4. **The ledger `Down` migration is not reversible once a result has been corrected.**
   `AddRatingChangeRevisions` cannot restore the old two-column unique index when a
   match already carries more than one revision, and it fails loudly rather than
   deleting rows to make room. That is the right trade — but rolling back past that
   migration needs the compensating rows removed by hand first.

## Suggested next work

Every item in `docs/Scope.md` is implemented. The last three — **A7** (tier
boundaries re-centred so `EloCalculator.BaseRating` sits in Bronze, no migration
because tiers are derived on every read), **D1** (team logos) and **D2**
(notifications) — landed on 2026-08-18; see
`docs/superpowers/specs/2026-08-18-logos-notifications-tiers-design.md` and the
plan beside it.

What is left is genuinely new work rather than unfinished work:

- **Refresh tokens.** The one open item from the security list — logout is still
  a no-op and a stolen token stays valid until it expires.
- **Double-elimination or group-stage brackets.** `BracketService` handles single
  elimination only.
- **Re-fit the A7 tier boundaries.** The current bands are a considered guess, not
  a fit to data; once there is enough seeded history, check whether Elite is
  actually rare and Bronze is not a trap.
- **An events table, if notifications need to grow.** The derived design covers
  the three request flows and structurally cannot cover anything else — see the
  notifications convention above.

Design specs and implementation plans for completed work live in `docs/superpowers/` and are worth reading before extending those areas.
