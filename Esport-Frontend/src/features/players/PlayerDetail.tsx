import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { playersApi } from "../../api/playersApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Pager, Skeleton, StatCard } from "../../components/ui/Primitives";
import { usePagedList } from "../../hooks/usePagedList";
import { MatchResultBadge } from "../matches/MatchResultBadge";
import type { PlayerMatchDto, PlayerProfileDto } from "../../types";

const LOG_PAGE_SIZE = 10;

const PlayerDetail = () => {
  const { id } = useParams();
  const playerId = Number(id);
  const { user } = useAuthStore();

  const [profile, setProfile] = useState<PlayerProfileDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [missing, setMissing] = useState(false);

  useEffect(() => {
    if (Number.isNaN(playerId)) {
      setMissing(true);
      setLoading(false);
      return;
    }

    let isActive = true;
    setLoading(true);

    playersApi
      .getProfile(playerId)
      .then((data) => isActive && setProfile(data))
      .catch(() => isActive && setMissing(true))
      .finally(() => isActive && setLoading(false));

    return () => {
      isActive = false;
    };
  }, [playerId]);

  const {
    items: log,
    page,
    setPage,
    totalCount,
    totalPages,
    loading: logLoading
  } = usePagedList<PlayerMatchDto>(
    (page, pageSize) => playersApi.getMatchLog(playerId, { page, pageSize }),
    String(playerId),
    LOG_PAGE_SIZE
  );

  if (missing) {
    return (
      <EmptyState
        title="Гравця не знайдено"
        hint="Можливо, профіль було видалено."
        action={
          <Link to="/players" className="btn btn-primary">
            До списку гравців
          </Link>
        }
      />
    );
  }

  const player = profile?.player;
  const canEdit = user?.role === "Admin" || user?.id === player?.userId;

  return (
    <>
      <PageHeader
        eyebrow={player?.position || "Гравець"}
        title={player?.nickname ?? `Гравець #${id}`}
        description={player?.team ? `${player.team.name} (${player.team.tag})` : "Вільний агент"}
        action={
          canEdit ? (
            <Link to={`/players/${id}/edit`} className="btn btn-secondary">
              Редагувати
            </Link>
          ) : undefined
        }
      />

      {loading && <Skeleton rows={2} />}

      {!loading && profile && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard
            label="Матчів"
            value={String(profile.matches)}
            hint={`${profile.wins}–${profile.losses} за результатами`}
            accent
          />
          <StatCard label="Відсоток перемог" value={`${profile.winRate}%`} />
          <StatCard
            label="K / D / A"
            value={`${profile.kills}/${profile.deaths}/${profile.assists}`}
          />
          <StatCard label="KDA" value={profile.kda.toFixed(2)} />
        </div>
      )}

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Матчі гравця</h2>
          <span className="tabular font-mono text-micro text-text-faint">{totalCount}</span>
        </div>
        <div className="panel-body space-y-4">
          {logLoading && <Skeleton rows={3} />}
          {!logLoading && log.length === 0 && (
            <EmptyState
              title="Ще немає зіграних матчів"
              hint="Записи зʼявляться, коли гравця внесуть до ростера матчу."
            />
          )}
          {!logLoading && log.length > 0 && (
            <div className="overflow-x-auto">
              <table className="table">
                <thead>
                  <tr>
                    <th>Результат</th>
                    <th>Дата</th>
                    <th>За кого</th>
                    <th>Суперник</th>
                    <th className="text-right">Рахунок</th>
                    <th className="text-right">K/D/A</th>
                    <th>Персонаж</th>
                  </tr>
                </thead>
                <tbody>
                  {log.map((entry) => (
                    <tr key={entry.matchId}>
                      <td>
                        <MatchResultBadge result={entry.result} />
                      </td>
                      <td className="tabular font-mono text-micro">
                        {new Date(entry.scheduledAt).toLocaleDateString("uk-UA", {
                          day: "2-digit",
                          month: "short",
                          year: "2-digit"
                        })}
                      </td>
                      <td>{entry.playedFor?.name ?? "—"}</td>
                      <td className="cell-primary">
                        <Link to={`/matches/${entry.matchId}`} className="hover:text-ember">
                          {entry.opponent?.name ?? "—"}
                        </Link>
                      </td>
                      <td className="tabular text-right font-mono">
                        {entry.teamScore}:{entry.opponentScore}
                      </td>
                      <td className="tabular text-right font-mono">
                        {entry.kills}/{entry.deaths}/{entry.assists}
                      </td>
                      <td>{entry.champion || "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <Pager
            page={page}
            pageSize={LOG_PAGE_SIZE}
            totalCount={totalCount}
            totalPages={totalPages}
            onChange={setPage}
            disabled={logLoading}
          />
        </div>
      </section>
    </>
  );
};

export default PlayerDetail;
