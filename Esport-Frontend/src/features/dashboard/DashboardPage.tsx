import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { CalendarClock, Coins, Swords, Trophy } from "lucide-react";
import { EmptyState, PageHeader, Skeleton, StatCard, StatusPill } from "../../components/ui/Primitives";
import { matchesApi } from "../../api/matchesApi";
import { tournamentsApi } from "../../api/tournamentsApi";
import type { MatchDto, TournamentStatsDto } from "../../types";

const formatPrize = (value: number) =>
  value >= 1000 ? `$${(value / 1000).toFixed(value % 1000 === 0 ? 0 : 1)}K` : `$${value}`;

const DashboardPage = () => {
  const [stats, setStats] = useState<TournamentStatsDto | null>(null);
  const [upcoming, setUpcoming] = useState<MatchDto[]>([]);
  const [live, setLive] = useState<MatchDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isActive = true;

    Promise.all([
      tournamentsApi.getStats().catch(() => null),
      matchesApi.getScheduled().catch(() => [] as MatchDto[]),
      matchesApi.getLive().catch(() => [] as MatchDto[])
    ])
      .then(([statsData, scheduled, liveData]) => {
        if (!isActive) {
          return;
        }
        setStats(statsData);
        setUpcoming(
          [...scheduled]
            .sort((a, b) => +new Date(a.scheduledAt) - +new Date(b.scheduledAt))
            .slice(0, 5)
        );
        setLive(liveData);
      })
      .finally(() => isActive && setLoading(false));

    return () => {
      isActive = false;
    };
  }, []);

  return (
    <>
      <PageHeader
        eyebrow="Командний центр"
        title="Огляд ліги"
        description="Що відбувається просто зараз і що почнеться найближчим часом."
        action={
          <Link to="/tournaments" className="btn btn-secondary">
            Усі турніри
          </Link>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label="Активні турніри"
          value={stats ? String(stats.activeTournaments) : "—"}
          hint={stats ? `Усього створено: ${stats.totalTournaments}` : undefined}
          icon={<Swords className="h-4 w-4" />}
          accent
        />
        <StatCard
          label="Відкрита реєстрація"
          value={stats ? String(stats.registrationOpen) : "—"}
          hint="Приймають заявки команд"
          icon={<CalendarClock className="h-4 w-4" />}
        />
        <StatCard
          label="Завершені"
          value={stats ? String(stats.completedTournaments) : "—"}
          hint="Сітку зіграно до фіналу"
          icon={<Trophy className="h-4 w-4" />}
        />
        <StatCard
          label="Призовий фонд"
          value={stats ? formatPrize(stats.totalPrizePool) : "—"}
          hint="Сумарно по всіх турнірах"
          icon={<Coins className="h-4 w-4" />}
        />
      </div>

      <div className="grid gap-5 lg:grid-cols-[1.4fr_1fr]">
        <section className="panel">
          <div className="panel-header">
            <h2 className="section-title">Найближчі матчі</h2>
            <Link to="/matches" className="btn btn-ghost btn-sm">
              Увесь розклад
            </Link>
          </div>
          <div className="panel-body">
            {loading && <Skeleton rows={3} />}
            {!loading && upcoming.length === 0 && (
              <EmptyState
                title="Розклад порожній"
                hint="Матчі зʼявляться тут, щойно організатор згенерує турнірну сітку."
                action={
                  <Link to="/tournaments" className="btn btn-primary">
                    Перейти до турнірів
                  </Link>
                }
              />
            )}
            {!loading && upcoming.length > 0 && (
              <ul className="divide-y divide-line-soft">
                {upcoming.map((match) => (
                  <li key={match.id} className="flex items-center justify-between gap-4 py-3 first:pt-0 last:pb-0">
                    <div className="min-w-0">
                      <div className="truncate text-body font-medium text-text">
                        {match.homeTeam?.name ?? "Очікується"}
                        <span className="mx-2 text-text-faint">проти</span>
                        {match.awayTeam?.name ?? "Очікується"}
                      </div>
                      <div className="tabular mt-0.5 font-mono text-micro text-text-faint">
                        {new Date(match.scheduledAt).toLocaleString("uk-UA", {
                          day: "2-digit",
                          month: "short",
                          hour: "2-digit",
                          minute: "2-digit"
                        })}
                      </div>
                    </div>
                    <span className="pill pill-neutral shrink-0 font-mono">{match.format}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </section>

        <section className="panel">
          <div className="panel-header">
            <h2 className="section-title">Зараз у грі</h2>
            {live.length > 0 && <span className="pill pill-live">{live.length}</span>}
          </div>
          <div className="panel-body">
            {loading && <Skeleton rows={2} />}
            {!loading && live.length === 0 && (
              <p className="muted text-body">Жоден матч зараз не триває. Тут зʼявляться live-результати.</p>
            )}
            {!loading &&
              live.map((match) => (
                <div key={match.id} className="surface-raised mb-3 px-4 py-3 last:mb-0">
                  <div className="flex items-center justify-between gap-3">
                    <span className="truncate text-body text-text">{match.homeTeam?.name ?? "—"}</span>
                    <span className="tabular font-mono text-h3 text-ember">{match.homeTeamScore}</span>
                  </div>
                  <div className="mt-1.5 flex items-center justify-between gap-3">
                    <span className="truncate text-body text-text">{match.awayTeam?.name ?? "—"}</span>
                    <span className="tabular font-mono text-h3 text-ember">{match.awayTeamScore}</span>
                  </div>
                  <div className="mt-3 flex items-center justify-between border-t border-line-soft pt-2.5">
                    <StatusPill status={match.status} />
                    <span className="font-mono text-micro text-text-faint">{match.format}</span>
                  </div>
                </div>
              ))}
          </div>
        </section>
      </div>
    </>
  );
};

export default DashboardPage;
