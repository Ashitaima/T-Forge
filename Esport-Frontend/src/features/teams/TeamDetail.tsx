import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { matchesApi } from "../../api/matchesApi";
import { membershipRequestsApi } from "../../api/membershipRequestsApi";
import { playersApi } from "../../api/playersApi";
import { teamsApi } from "../../api/teamsApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Pager, Skeleton } from "../../components/ui/Primitives";
import { usePagedList } from "../../hooks/usePagedList";
import { MatchResultBadge } from "../matches/MatchResultBadge";
import type { MatchDto, MembershipRequestDto, PlayerDto, TeamDto, TeamSummaryStatsDto } from "../../types";

const HISTORY_PAGE_SIZE = 10;

const TeamDetail = () => {
  const { id } = useParams();
  const teamId = Number(id);
  const { user } = useAuthStore();

  const [team, setTeam] = useState<TeamDto | null>(null);
  const [summary, setSummary] = useState<TeamSummaryStatsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [missing, setMissing] = useState(false);

  // Профіль і підсумок не залежать від сторінки історії
  const loadTeam = useCallback(() => {
    if (Number.isNaN(teamId)) {
      setMissing(true);
      setLoading(false);
      return;
    }

    setLoading(true);

    Promise.all([
      teamsApi.getWithPlayers(teamId),
      teamsApi.getSummary(teamId).catch(() => null)
    ])
      .then(([teamData, summaryData]) => {
        setTeam(teamData);
        setSummary(summaryData);
      })
      .catch(() => setMissing(true))
      .finally(() => setLoading(false));
  }, [teamId]);

  useEffect(() => {
    loadTeam();
  }, [loadTeam]);

  const {
    items: matches,
    page,
    setPage,
    totalCount,
    totalPages,
    loading: historyLoading
  } = usePagedList<MatchDto>(
    (page, pageSize) =>
      matchesApi.getPaged({ page, pageSize, teamId, sortBy: "scheduledAt", sortDirection: "desc" }),
    String(teamId),
    HISTORY_PAGE_SIZE
  );

  const [myPlayer, setMyPlayer] = useState<PlayerDto | null>(null);
  const [requests, setRequests] = useState<MembershipRequestDto[]>([]);

  const isCaptain = Boolean(user && team?.captain?.id === user.id);
  const isAdmin = user?.role === "Admin";

  useEffect(() => {
    playersApi
      .getMe()
      .then(setMyPlayer)
      .catch(() => setMyPlayer(null)); // користувач без профілю гравця — це нормально
  }, []);

  const loadRequests = useCallback(() => {
    if (Number.isNaN(teamId) || !(isCaptain || isAdmin)) {
      setRequests([]);
      return;
    }

    membershipRequestsApi
      .getForTeam(teamId, "Pending")
      .then(setRequests)
      .catch(() => setRequests([]));
  }, [teamId, isCaptain, isAdmin]);

  useEffect(() => {
    loadRequests();
  }, [loadRequests]);

  const applications = requests.filter((r) => r.direction === "Application");
  const invitations = requests.filter((r) => r.direction === "Invite");

  const respond = async (action: "accept" | "decline" | "cancel", requestId: number) => {
    await membershipRequestsApi[action](requestId);
    loadRequests();
    loadTeam(); // прийнята заявка змінює склад — перечитуємо команду
  };

  const [freeAgentSearch, setFreeAgentSearch] = useState("");
  const [freeAgents, setFreeAgents] = useState<PlayerDto[]>([]);

  useEffect(() => {
    if (!(isCaptain || isAdmin)) {
      return;
    }

    playersApi
      .getPaged({ freeAgents: true, search: freeAgentSearch, page: 1, pageSize: 10 })
      .then((response) => setFreeAgents(response.data))
      .catch(() => setFreeAgents([]));
  }, [freeAgentSearch, isCaptain, isAdmin]);

  const invite = async (playerId: number) => {
    await membershipRequestsApi.invite(teamId, playerId);
    loadRequests();
  };

  const canApply =
    Boolean(myPlayer) && !myPlayer?.team && !isCaptain && Boolean(team?.isActive);

  const [myPendingApplication, setMyPendingApplication] = useState<MembershipRequestDto | null>(null);

  useEffect(() => {
    if (!myPlayer) {
      setMyPendingApplication(null);
      return;
    }

    membershipRequestsApi
      .getForPlayer(myPlayer.id, "Pending")
      .then((rows) => setMyPendingApplication(rows.find((r) => r.teamId === teamId) ?? null))
      .catch(() => setMyPendingApplication(null));
  }, [myPlayer, teamId]);

  const apply = async () => {
    if (!myPlayer) {
      return;
    }

    const created = await membershipRequestsApi.apply(myPlayer.id, teamId);
    setMyPendingApplication(created);
  };

  if (missing) {
    return (
      <EmptyState
        title="Команду не знайдено"
        hint="Можливо, її було видалено."
        action={
          <Link to="/teams" className="btn btn-primary">
            До списку команд
          </Link>
        }
      />
    );
  }

  const resultFor = (match: MatchDto) => {
    if (match.status !== "Completed" || !match.winnerTeam) {
      return "Pending";
    }
    return match.winnerTeam.id === teamId ? "Win" : "Loss";
  };

  return (
    <>
      <PageHeader
        eyebrow={team?.tag}
        title={team?.name ?? `Команда #${id}`}
        description={team?.description}
        action={
          isCaptain ? (
            <Link to={`/teams/${id}/edit`} className="btn btn-secondary">
              Редагувати
            </Link>
          ) : myPendingApplication ? (
            <div className="flex items-center gap-3">
              <span className="pill">Заявку надіслано</span>
              <button
                className="btn btn-ghost btn-sm"
                onClick={async () => {
                  await membershipRequestsApi.cancel(myPendingApplication.id);
                  setMyPendingApplication(null);
                }}
              >
                Скасувати
              </button>
            </div>
          ) : canApply ? (
            <button className="btn btn-primary" onClick={apply}>
              Подати заявку
            </button>
          ) : undefined
        }
      />

      <p className="text-body text-text-muted">
        Капітан{" "}
        <span className="text-text">
          {team?.captain ? `@${team.captain.username}` : "не призначено"}
        </span>
        {team?.region && (
          <>
            {" "}
            · Регіон <span className="text-text">{team.region}</span>
          </>
        )}
        {" "}
        · Склад <span className="tabular font-mono text-text">{team?.players?.length ?? 0}</span>
      </p>

      {summary && (
        <div className="surface-raised flex flex-wrap items-center gap-x-8 gap-y-3 px-5 py-4">
          <span className="text-body text-text-muted">
            Ігор <span className="tabular font-mono text-text">{summary.played}</span>
          </span>
          <span className="text-body text-text-muted">
            Перемог <span className="tabular font-mono text-win">{summary.wins}</span>
          </span>
          <span className="text-body text-text-muted">
            Поразок <span className="tabular font-mono text-text">{summary.losses}</span>
          </span>
          <span className="text-body text-text-muted">
            Відсоток <span className="tabular font-mono text-text">{summary.winRate}%</span>
          </span>
          {summary.streak && (
            <span className="text-body text-text-muted">
              Серія{" "}
              <span
                className={`tabular font-mono ${summary.streak.type === "Win" ? "text-win" : "text-danger"}`}
              >
                {summary.streak.count} {summary.streak.type === "Win" ? "П" : "Пр"}
              </span>
            </span>
          )}
        </div>
      )}

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Історія матчів</h2>
          <span className="tabular font-mono text-micro text-text-faint">{totalCount}</span>
        </div>
        <div className="panel-body space-y-4">
          {historyLoading && <Skeleton rows={3} />}
          {!historyLoading && matches.length === 0 && (
            <EmptyState
              title="Матчів ще не було"
              hint="Історія зʼявиться після першого зіграного матчу команди."
            />
          )}
          {!historyLoading && matches.length > 0 && (
            <ul className="divide-y divide-line-soft">
              {matches.map((match) => {
                const isHome = match.homeTeam?.id === teamId;
                const opponent = isHome ? match.awayTeam : match.homeTeam;
                const own = isHome ? match.homeTeamScore : match.awayTeamScore;
                const other = isHome ? match.awayTeamScore : match.homeTeamScore;

                return (
                  <li key={match.id} className="flex flex-wrap items-center gap-x-5 gap-y-2 py-3">
                    <MatchResultBadge result={resultFor(match)} />
                    <span className="tabular w-20 font-mono text-micro text-text-faint">
                      {new Date(match.scheduledAt).toLocaleDateString("uk-UA", {
                        day: "2-digit",
                        month: "short"
                      })}
                    </span>
                    <Link
                      to={`/matches/${match.id}`}
                      className="min-w-0 flex-1 truncate text-body text-text hover:text-ember"
                    >
                      {opponent?.name ?? "Очікується"}
                    </Link>
                    <span className="tabular font-mono text-body text-text">
                      {own}:{other}
                    </span>
                    <span className="w-full text-micro text-text-faint sm:w-auto">
                      {match.tournament?.name ?? "—"} · {match.matchType}
                    </span>
                  </li>
                );
              })}
            </ul>
          )}
          <Pager
            page={page}
            pageSize={HISTORY_PAGE_SIZE}
            totalCount={totalCount}
            totalPages={totalPages}
            onChange={setPage}
            disabled={historyLoading}
          />
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Склад</h2>
        </div>
        <div className="panel-body">
          {loading && <Skeleton rows={3} />}
          {!loading && (team?.players?.length ?? 0) === 0 && (
            <EmptyState
              title="Склад порожній"
              hint="Гравці зʼявляться тут, щойно капітан надішле запрошення або схвалить заявку."
            />
          )}
          {!loading && (team?.players?.length ?? 0) > 0 && (
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              {team?.players?.map((player) => (
                <div key={player.id} className="surface-raised px-4 py-3.5">
                  <div className="flex items-start justify-between gap-2">
                    <span className="truncate text-body font-medium text-text">{player.nickname}</span>
                    {!player.isActive && <span className="pill pill-off shrink-0">Неактивний</span>}
                  </div>
                  <div className="muted mt-1 text-micro">{player.position || "Позиція не вказана"}</div>
                  {player.country && (
                    <div className="mt-2 font-mono text-micro text-text-faint">{player.country}</div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </section>

      {(isCaptain || isAdmin) && (
        <section className="panel mt-6">
          <div className="eyebrow">Заявки на вступ</div>

          {applications.length === 0 && (
            <p className="muted mt-3 text-micro">Немає нових заявок.</p>
          )}

          {applications.map((request) => (
            <div
              key={request.id}
              className="surface-raised mt-3 flex items-center justify-between px-4 py-3.5"
            >
              <div className="min-w-0">
                <div className="truncate text-body font-medium text-text">
                  {request.playerNickname}
                </div>
                <div className="muted mt-1 text-micro">
                  {request.playerPosition || "Позиція не вказана"}
                </div>
              </div>
              <div className="flex shrink-0 gap-2">
                <button className="btn btn-primary btn-sm" onClick={() => respond("accept", request.id)}>
                  Прийняти
                </button>
                <button className="btn btn-ghost btn-sm" onClick={() => respond("decline", request.id)}>
                  Відхилити
                </button>
              </div>
            </div>
          ))}
        </section>
      )}

      {(isCaptain || isAdmin) && invitations.length > 0 && (
        <section className="panel mt-6">
          <div className="eyebrow">Надіслані запрошення</div>

          {invitations.map((request) => (
            <div
              key={request.id}
              className="surface-raised mt-3 flex items-center justify-between px-4 py-3.5"
            >
              <div className="min-w-0">
                <div className="truncate text-body font-medium text-text">
                  {request.playerNickname}
                </div>
                <span className="pill mt-1">Очікує відповіді</span>
              </div>
              <button className="btn btn-ghost btn-sm" onClick={() => respond("cancel", request.id)}>
                Скасувати
              </button>
            </div>
          ))}
        </section>
      )}

      {(isCaptain || isAdmin) && (
        <section className="panel mt-6">
          <div className="eyebrow">Запросити гравця</div>

          <input
            className="input mt-3 w-full"
            placeholder="Пошук вільних гравців"
            value={freeAgentSearch}
            onChange={(event) => setFreeAgentSearch(event.target.value)}
          />

          {freeAgents.length === 0 && (
            <p className="muted mt-3 text-micro">Вільних гравців не знайдено.</p>
          )}

          {freeAgents.map((player) => (
            <div
              key={player.id}
              className="surface-raised mt-3 flex items-center justify-between px-4 py-3.5"
            >
              <span className="truncate text-body text-text">{player.nickname}</span>
              <button className="btn btn-secondary btn-sm" onClick={() => invite(player.id)}>
                Запросити
              </button>
            </div>
          ))}
        </section>
      )}
    </>
  );
};

export default TeamDetail;
