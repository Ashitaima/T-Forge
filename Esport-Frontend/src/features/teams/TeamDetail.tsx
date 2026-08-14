import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { matchesApi } from "../../api/matchesApi";
import { teamsApi } from "../../api/teamsApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Pager, Skeleton } from "../../components/ui/Primitives";
import { usePagedList } from "../../hooks/usePagedList";
import { MatchResultBadge } from "../matches/MatchResultBadge";
import type { MatchDto, TeamDto, TeamSummaryStatsDto } from "../../types";

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
  useEffect(() => {
    if (Number.isNaN(teamId)) {
      setMissing(true);
      setLoading(false);
      return;
    }

    let isActive = true;
    setLoading(true);

    Promise.all([
      teamsApi.getWithPlayers(teamId),
      teamsApi.getSummary(teamId).catch(() => null)
    ])
      .then(([teamData, summaryData]) => {
        if (!isActive) {
          return;
        }
        setTeam(teamData);
        setSummary(summaryData);
      })
      .catch(() => isActive && setMissing(true))
      .finally(() => isActive && setLoading(false));

    return () => {
      isActive = false;
    };
  }, [teamId]);

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

  const isCaptain = user?.id === team?.captain?.id;

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
              hint="Гравці зʼявляться тут, щойно капітан додасть їх до команди."
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
    </>
  );
};

export default TeamDetail;
