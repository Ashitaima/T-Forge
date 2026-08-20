import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { AlertCircle, CheckCircle2, Lock, PlusCircle, Users } from "lucide-react";
import { matchesApi } from "../../api/matchesApi";
import { teamsApi } from "../../api/teamsApi";
import { tournamentInvitationsApi } from "../../api/tournamentInvitationsApi";
import { tournamentsApi } from "../../api/tournamentsApi";
import { useAuthStore } from "../../store/authStore";
import { useIsRole } from "../../hooks/useEffectiveRole";
import { EmptyState, PageHeader, Skeleton, StatusPill } from "../../components/ui/Primitives";
import { Avatar, teamInitials } from "../../components/ui/Avatar";
import { BracketView } from "./BracketView";
import { MatchRow } from "../matches/MatchesSchedule";
import type { MatchDto, TeamRowDto, TeamSummaryDto, TournamentDto,
  TournamentInvitationDto, TournamentStandingDto } from "../../types";

const TournamentDetail = () => {
  const { id } = useParams();
  const tournamentId = Number(id);
  const { user } = useAuthStore();

  const [tournament, setTournament] = useState<TournamentDto | null>(null);
  const [matches, setMatches] = useState<MatchDto[]>([]);
  const [registered, setRegistered] = useState<TeamSummaryDto[]>([]);
  const [allTeams, setAllTeams] = useState<TeamRowDto[]>([]);
  const [standings, setStandings] = useState<TournamentStandingDto[]>([]);
  const [invitations, setInvitations] = useState<TournamentInvitationDto[]>([]);
  const [selectedTeam, setSelectedTeam] = useState("");
  const [inviteTeam, setInviteTeam] = useState("");
  const [applyTeam, setApplyTeam] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  // Роль читаємо через хук (щоб діяв режим розробника), а власника турніру —
  // за справжнім id: підміна ролі не повинна робити вас чужим організатором.
  const isAdmin = useIsRole("Admin");
  const isOrganizer = useIsRole("Organizer");
  const canManage = isAdmin || (isOrganizer && tournament?.organizer?.id === user?.id);
  const isRegistrationOpen = tournament?.status === "Registration";

  const load = useCallback(async () => {
    if (!tournamentId) {
      return;
    }

    setLoading(true);
    try {
      const [tournamentData, matchesData, registeredData, teamsData, standingsData, invitationData] =
        await Promise.all([
          tournamentsApi.getById(tournamentId),
          matchesApi.getPaged({ page: 1, pageSize: 64, tournamentId }),
          tournamentsApi.getRegisteredTeams(tournamentId).catch(() => [] as TeamSummaryDto[]),
          teamsApi.getPaged({ page: 1, pageSize: 100 }).then((r) => r.data).catch(() => [] as TeamRowDto[]),
          tournamentsApi.getStandings(tournamentId).catch(() => [] as TournamentStandingDto[]),
          tournamentInvitationsApi
            .getForTournament(tournamentId, "Pending")
            .catch(() => [] as TournamentInvitationDto[])
        ]);
      setTournament(tournamentData);
      setMatches(matchesData.data);
      setRegistered(registeredData);
      setAllTeams(teamsData);
      setStandings(standingsData);
      setInvitations(invitationData);
    } finally {
      setLoading(false);
    }
  }, [tournamentId]);

  useEffect(() => {
    load();
  }, [load]);

  // Бекенд повертає зрозумілі повідомлення про помилки — показуємо їх як є
  const run = async (action: () => Promise<unknown>, successMessage: string) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await action();
      setNotice(successMessage);
      await load();
    } catch (err: unknown) {
      const response = (err as { response?: { data?: { message?: string } } }).response;
      setError(response?.data?.message ?? "Не вдалося виконати дію");
    } finally {
      setBusy(false);
    }
  };

  const availableTeams = useMemo(
    () => allTeams.filter((team) => !registered.some((r) => r.id === team.id)),
    [allTeams, registered]
  );

  // Кого ще можна запросити: не зареєстрованих і без відкритого запиту —
  // повторне запрошення сервер усе одно відхилить.
  const invitableTeams = useMemo(
    () => availableTeams.filter((team) => !invitations.some((i) => i.teamId === team.id)),
    [availableTeams, invitations]
  );

  // Команди, за які може подати заявку саме цей користувач.
  const myTeams = useMemo(
    () => invitableTeams.filter((team) => team.captainId === user?.id),
    [invitableTeams, user?.id]
  );

  const incomingApplications = invitations.filter((i) => i.direction === "Application");
  const sentInvitations = invitations.filter((i) => i.direction === "Invite");

  const isInviteOnly = Boolean(tournament?.isInviteOnly);
  const hasBracket = matches.some((match) => match.round > 0);

  // Раунд, потім час: сітка читається згори вниз, а матчі поза сіткою
  // (Round = 0) стають першими, бо саме їх призначають вручну.
  const tournamentMatches = [...matches].sort(
    (left, right) =>
      left.round - right.round ||
      new Date(left.scheduledAt).getTime() - new Date(right.scheduledAt).getTime()
  );

  const removeMatch = async (matchId: number) => {
    await matchesApi.remove(matchId);
    load();
  };
  const slotsLeft = tournament ? tournament.maxTeams - registered.length : 0;

  return (
    <>
      <PageHeader
        eyebrow={tournament?.game}
        title={tournament?.name ?? `Турнір #${id}`}
        description={tournament?.description}
        action={
          canManage ? (
            <Link to={`/tournaments/${id}/edit`} className="btn btn-secondary">
              Редагувати
            </Link>
          ) : undefined
        }
      />

      {tournament && (
        <div className="flex flex-wrap items-center gap-x-6 gap-y-3">
          <StatusPill status={tournament.status} />
          {isInviteOnly && (
            <span className="pill pill-neutral" title="Склад учасників визначає організатор">
              <Lock className="h-3 w-3" />
              Тільки за запрошеннями
            </span>
          )}
          <span className="flex items-center gap-2 text-body text-text-muted">
            <Users className="h-4 w-4 text-text-faint" />
            <span className="tabular font-mono text-text">
              {registered.length}/{tournament.maxTeams}
            </span>
            команд
          </span>
          <span className="text-body text-text-muted">
            Призовий фонд{" "}
            <span className="tabular font-mono text-text">${tournament.prizePool}</span>
          </span>
          <span className="tabular font-mono text-micro text-text-faint">
            {new Date(tournament.startDate).toLocaleDateString("uk-UA")} —{" "}
            {new Date(tournament.endDate).toLocaleDateString("uk-UA")}
          </span>
        </div>
      )}

      {error && (
        <div className="notice notice-error">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}
      {notice && (
        <div className="notice notice-success">
          <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" />
          <span>{notice}</span>
        </div>
      )}

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Турнірна сітка</h2>
          {canManage && isRegistrationOpen && (
            <button
              type="button"
              disabled={busy || registered.length < 2}
              onClick={() => run(() => tournamentsApi.generateBracket(tournamentId), "Турнірну сітку створено")}
              className="btn btn-primary"
            >
              Згенерувати сітку
            </button>
          )}
        </div>
        <div className="panel-body">
          {loading && <Skeleton rows={2} />}
          {!loading && !hasBracket && (
            <EmptyState
              title="Сітку ще не створено"
              hint={
                canManage
                  ? "Зареєструйте щонайменше дві команди, потім згенеруйте сітку — вона розставить пари й розклад автоматично."
                  : "Організатор згенерує сітку, коли реєстрація завершиться."
              }
            />
          )}
          {!loading && hasBracket && <BracketView matches={matches} />}
        </div>
      </section>

      {/* Матчі турніру живуть тут, а не в загальному розкладі: там вони
          губилися серед чужих, і «матчі цього турніру» доводилося збирати
          фільтром. Сітка показує лише пари з раундів, тож список потрібен
          окремо — матч, доданий організатором поза сіткою, має Round = 0. */}
      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Матчі турніру</h2>
          <div className="flex items-center gap-3">
            <span className="tabular font-mono text-micro text-text-faint">{matches.length}</span>
            {canManage && (
              <Link to={`/tournaments/${tournamentId}/matches/new`} className="btn btn-secondary btn-sm">
                <PlusCircle className="h-4 w-4" />
                Додати матч
              </Link>
            )}
          </div>
        </div>
        <div className="panel-body space-y-3">
          {loading && <Skeleton rows={3} />}

          {!loading && matches.length === 0 && (
            <EmptyState
              title="Матчів ще немає"
              hint={
                canManage
                  ? "Згенеруйте сітку або додайте матч вручну."
                  : "Вони зʼявляться, щойно організатор складе розклад."
              }
            />
          )}

          {!loading &&
            tournamentMatches.map((match) => (
              <MatchRow
                key={match.id}
                match={match}
                showScore={match.status === "Completed"}
                canEdit={canManage}
                onDelete={removeMatch}
              />
            ))}
        </div>
      </section>

      {hasBracket && standings.length > 0 && (
        <section className="panel overflow-x-auto">
          <div className="panel-header">
            <h2 className="section-title">Підсумкова таблиця</h2>
          </div>
          <table className="table">
            <thead>
              <tr>
                <th className="w-px">#</th>
                <th>Команда</th>
                <th>Результат</th>
                <th className="text-right">Ігор</th>
                <th className="text-right">В</th>
                <th className="text-right">П</th>
              </tr>
            </thead>
            <tbody>
              {standings.map((row) => (
                <tr key={row.team?.id}>
                  <td>
                    <span className={`tabular font-mono ${row.place === 1 ? "text-ember" : "text-text-faint"}`}>
                      {String(row.place).padStart(2, "0")}
                    </span>
                  </td>
                  <td className="cell-primary">
                    <Link to={`/teams/${row.team?.id}`} className="hover:text-ember">
                      {row.team?.name}
                    </Link>
                  </td>
                  <td>
                    <span className={`pill ${row.place === 1 ? "pill-live" : "pill-neutral"}`}>
                      {row.outcome}
                    </span>
                  </td>
                  <td className="tabular text-right font-mono">{row.played}</td>
                  <td className="tabular text-right font-mono text-win">{row.wins}</td>
                  <td className="tabular text-right font-mono">{row.losses}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Учасники</h2>
          {isRegistrationOpen && (
            <span className="font-mono text-micro text-text-faint">
              {slotsLeft > 0 ? `вільних місць: ${slotsLeft}` : "місць немає"}
            </span>
          )}
        </div>

        {/* На відкритому турнірі капітан реєструє команду одним рухом. На
            закритому пряма реєстрація лишається тільки в організатора —
            решта проходить через запрошення чи заявку. */}
        {isRegistrationOpen && (!isInviteOnly || canManage) && (
          <div className="flex flex-wrap gap-3 border-b border-line-soft px-5 py-4">
            <label className="sr-only" htmlFor="team-picker">
              Команда для реєстрації
            </label>
            <select
              id="team-picker"
              value={selectedTeam}
              onChange={(event) => setSelectedTeam(event.target.value)}
              className="input min-w-[15rem] flex-1"
            >
              <option value="">Оберіть команду</option>
              {availableTeams.map((team) => (
                <option key={team.id} value={team.id}>
                  {team.name} ({team.tag})
                </option>
              ))}
            </select>
            <button
              type="button"
              disabled={!selectedTeam || busy || slotsLeft <= 0}
              onClick={() =>
                run(
                  () => tournamentsApi.registerTeam(tournamentId, Number(selectedTeam)),
                  "Команду зареєстровано"
                ).then(() => setSelectedTeam(""))
              }
              className="btn btn-primary"
            >
              Зареєструвати
            </button>
          </div>
        )}

        {/* Капітан на закритому турнірі: подати заявку */}
        {isRegistrationOpen && isInviteOnly && !canManage && myTeams.length > 0 && (
          <div className="flex flex-wrap gap-3 border-b border-line-soft px-5 py-4">
            <label className="sr-only" htmlFor="apply-picker">
              Команда для заявки
            </label>
            <select
              id="apply-picker"
              value={applyTeam}
              onChange={(event) => setApplyTeam(event.target.value)}
              className="input min-w-[15rem] flex-1"
            >
              <option value="">Оберіть свою команду</option>
              {myTeams.map((team) => (
                <option key={team.id} value={team.id}>
                  {team.name} ({team.tag})
                </option>
              ))}
            </select>
            <button
              type="button"
              disabled={!applyTeam || busy}
              onClick={() =>
                run(
                  () => tournamentInvitationsApi.apply(tournamentId, Number(applyTeam)),
                  "Заявку надіслано — очікуйте рішення організатора"
                ).then(() => setApplyTeam(""))
              }
              className="btn btn-primary"
            >
              Подати заявку
            </button>
          </div>
        )}

        <div className="panel-body">
          {loading && <Skeleton rows={2} />}
          {!loading && registered.length === 0 && (
            <EmptyState
              title="Ще немає заявок"
              hint="Капітан команди або організатор може зареєструвати склад, поки триває реєстрація."
            />
          )}
          {!loading && registered.length > 0 && (
            <ol className="divide-y divide-line-soft">
              {registered.map((team, index) => (
                <li key={team.id} className="flex items-center gap-4 py-3 first:pt-0 last:pb-0">
                  {/* Номер посіву — реальна інформація: він визначає пари у сітці */}
                  <span className="tabular w-6 shrink-0 font-mono text-micro text-text-faint">
                    {String(index + 1).padStart(2, "0")}
                  </span>
                  <Avatar
                    url={team.logoPath}
                    shape="square"
                    size="sm"
                    fallback={teamInitials(team.name, team.tag)}
                    alt=""
                  />
                  <div className="min-w-0 flex-1">
                    <div className="truncate text-body font-medium text-text">{team.name}</div>
                    <div className="font-mono text-micro text-text-faint">
                      {team.tag}
                      {team.region ? ` · ${team.region}` : ""}
                    </div>
                  </div>
                  {isRegistrationOpen && (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => run(() => tournamentsApi.withdrawTeam(tournamentId, team.id), "Команду знято")}
                      className="btn btn-ghost btn-sm"
                    >
                      Зняти
                    </button>
                  )}
                </li>
              ))}
            </ol>
          )}
        </div>
      </section>

      {/* Запрошення та заявки — інструмент організатора, тож і панель його. */}
      {canManage && isRegistrationOpen && (
        <section className="panel">
          <div className="panel-header">
            <h2 className="section-title">Запрошення та заявки</h2>
            <span className="font-mono text-micro text-text-faint">{invitations.length}</span>
          </div>

          <div className="flex flex-wrap gap-3 border-b border-line-soft px-5 py-4">
            <label className="sr-only" htmlFor="invite-picker">
              Команда для запрошення
            </label>
            <select
              id="invite-picker"
              value={inviteTeam}
              onChange={(event) => setInviteTeam(event.target.value)}
              className="input min-w-[15rem] flex-1"
            >
              <option value="">Оберіть команду</option>
              {invitableTeams.map((team) => (
                <option key={team.id} value={team.id}>
                  {team.name} ({team.tag})
                </option>
              ))}
            </select>
            <button
              type="button"
              disabled={!inviteTeam || busy || slotsLeft <= 0}
              onClick={() =>
                run(
                  () => tournamentInvitationsApi.invite(tournamentId, Number(inviteTeam)),
                  "Запрошення надіслано"
                ).then(() => setInviteTeam(""))
              }
              className="btn btn-secondary"
            >
              Запросити
            </button>
          </div>

          <div className="panel-body space-y-5">
            <div>
              <div className="eyebrow mb-1">Заявки від команд</div>
              {incomingApplications.length === 0 ? (
                <p className="muted text-micro">Нових заявок немає.</p>
              ) : (
                incomingApplications.map((invitation) => (
                  <div
                    key={invitation.id}
                    className="surface-raised mt-3 flex flex-wrap items-center justify-between gap-3 px-4 py-3.5"
                  >
                    <div className="min-w-0">
                      <Link
                        to={`/teams/${invitation.teamId}`}
                        className="truncate text-body font-medium text-text hover:text-ember"
                      >
                        {invitation.teamName}
                      </Link>
                      <div className="muted mt-1 font-mono text-micro">{invitation.teamTag}</div>
                      {invitation.message && (
                        <div className="muted mt-1 text-micro">{invitation.message}</div>
                      )}
                    </div>
                    <div className="flex shrink-0 gap-2">
                      <button
                        type="button"
                        disabled={busy || slotsLeft <= 0}
                        className="btn btn-primary btn-sm"
                        onClick={() =>
                          run(
                            () => tournamentInvitationsApi.accept(invitation.id),
                            "Команду прийнято на турнір"
                          )
                        }
                      >
                        Прийняти
                      </button>
                      <button
                        type="button"
                        disabled={busy}
                        className="btn btn-ghost btn-sm"
                        onClick={() =>
                          run(() => tournamentInvitationsApi.decline(invitation.id), "Заявку відхилено")
                        }
                      >
                        Відхилити
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>

            {sentInvitations.length > 0 && (
              <div className="border-t border-line-soft pt-4">
                <div className="eyebrow mb-1">Надіслані запрошення</div>
                {sentInvitations.map((invitation) => (
                  <div
                    key={invitation.id}
                    className="surface-raised mt-3 flex flex-wrap items-center justify-between gap-3 px-4 py-3.5"
                  >
                    <div className="min-w-0">
                      <Link
                        to={`/teams/${invitation.teamId}`}
                        className="truncate text-body font-medium text-text hover:text-ember"
                      >
                        {invitation.teamName}
                      </Link>
                      <span className="pill mt-1">Очікує відповіді</span>
                    </div>
                    <button
                      type="button"
                      disabled={busy}
                      className="btn btn-ghost btn-sm"
                      onClick={() =>
                        run(() => tournamentInvitationsApi.cancel(invitation.id), "Запрошення скасовано")
                      }
                    >
                      Скасувати
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </section>
      )}
    </>
  );
};

export default TournamentDetail;
