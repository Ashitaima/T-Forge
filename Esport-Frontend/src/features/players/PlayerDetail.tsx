import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { AlertCircle } from "lucide-react";
import { membershipRequestsApi } from "../../api/membershipRequestsApi";
import { playersApi } from "../../api/playersApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Pager, Skeleton, StatCard } from "../../components/ui/Primitives";
import { usePagedList } from "../../hooks/usePagedList";
import { MatchResultBadge } from "../matches/MatchResultBadge";
import type { MembershipRequestDto, PlayerMatchDto, PlayerProfileDto } from "../../types";

const LOG_PAGE_SIZE = 10;

const PlayerDetail = () => {
  const { id } = useParams();
  const playerId = Number(id);
  const { user } = useAuthStore();

  const [profile, setProfile] = useState<PlayerProfileDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [missing, setMissing] = useState(false);

  const profileToken = useRef(0);

  const loadProfile = useCallback(() => {
    if (Number.isNaN(playerId)) {
      setMissing(true);
      setLoading(false);
      return;
    }

    const token = ++profileToken.current;
    setLoading(true);

    playersApi
      .getProfile(playerId)
      .then((data) => {
        if (token === profileToken.current) {
          setProfile(data);
        }
      })
      .catch(() => {
        if (token === profileToken.current) {
          setMissing(true);
        }
      })
      .finally(() => {
        if (token === profileToken.current) {
          setLoading(false);
        }
      });
  }, [playerId]);

  useEffect(() => {
    loadProfile();
  }, [loadProfile]);

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

  const [requests, setRequests] = useState<MembershipRequestDto[]>([]);

  const isOwnProfile = Boolean(user && profile?.player?.userId === user.id);

  const requestsToken = useRef(0);

  const loadRequests = useCallback(() => {
    if (Number.isNaN(playerId) || !isOwnProfile) {
      setRequests([]);
      return;
    }

    const token = ++requestsToken.current;

    membershipRequestsApi
      .getForPlayer(playerId, "Pending")
      .then((data) => {
        if (token === requestsToken.current) {
          setRequests(data);
        }
      })
      .catch(() => {
        if (token === requestsToken.current) {
          setRequests([]);
        }
      });
  }, [playerId, isOwnProfile]);

  useEffect(() => {
    loadRequests();
  }, [loadRequests]);

  const invitations = requests.filter((r) => r.direction === "Invite");
  const applications = requests.filter((r) => r.direction === "Application");

  const [actionError, setActionError] = useState<string | null>(null);

  // Бекенд повертає зрозумілі повідомлення про помилки — показуємо їх як є
  const runAction = async (action: () => Promise<unknown>) => {
    setActionError(null);
    try {
      await action();
    } catch (err: unknown) {
      const response = (err as { response?: { data?: { message?: string } } }).response;
      setActionError(response?.data?.message ?? "Не вдалося виконати дію");
    }
  };

  const respond = (action: "accept" | "decline" | "cancel", requestId: number) =>
    runAction(async () => {
      await membershipRequestsApi[action](requestId);
      loadRequests();
      loadProfile(); // прийняте запрошення змінює команду гравця
    });

  const leaveTeam = () =>
    runAction(async () => {
      await playersApi.leaveTeam(playerId);
      loadRequests();
      loadProfile(); // вихід із команди звільняє профіль для нових запрошень
    });

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
          canEdit || (isOwnProfile && player?.team) ? (
            <div className="flex items-center gap-3">
              {canEdit && (
                <Link to={`/players/${id}/edit`} className="btn btn-secondary">
                  Редагувати
                </Link>
              )}
              {isOwnProfile && player?.team && (
                <button type="button" className="btn btn-danger" onClick={leaveTeam}>
                  Покинути команду
                </button>
              )}
            </div>
          ) : undefined
        }
      />

      {actionError && (
        <div className="notice notice-error">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          <span>{actionError}</span>
        </div>
      )}

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

      {isOwnProfile && invitations.length > 0 && (
        <section className="panel mt-6">
          <div className="eyebrow">Ваші запрошення</div>

          {invitations.map((request) => (
            <div
              key={request.id}
              className="surface-raised mt-3 flex items-center justify-between px-4 py-3.5"
            >
              <div className="min-w-0">
                <div className="truncate text-body font-medium text-text">{request.teamName}</div>
                <div className="muted mt-1 font-mono text-micro">{request.teamTag}</div>
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

      {isOwnProfile && applications.length > 0 && (
        <section className="panel mt-6">
          <div className="eyebrow">Надіслані заявки</div>

          {applications.map((request) => (
            <div
              key={request.id}
              className="surface-raised mt-3 flex items-center justify-between px-4 py-3.5"
            >
              <div className="min-w-0">
                <div className="truncate text-body font-medium text-text">{request.teamName}</div>
                <span className="pill mt-1">Очікує відповіді</span>
              </div>
              <button className="btn btn-ghost btn-sm" onClick={() => respond("cancel", request.id)}>
                Скасувати
              </button>
            </div>
          ))}
        </section>
      )}
    </>
  );
};

export default PlayerDetail;
