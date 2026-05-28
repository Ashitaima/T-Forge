import { useEffect, useState } from "react";
import { CalendarClock, Trophy, UsersRound } from "lucide-react";
import { StatCard } from "../../components/ui/StatCard";
import { tournamentsApi } from "../../api/tournamentsApi";
import type { TournamentStatsDto } from "../../types";

const DashboardPage = () => {
  const [stats, setStats] = useState<TournamentStatsDto | null>(null);

  useEffect(() => {
    let isActive = true;
    const load = async () => {
      try {
        const response = await tournamentsApi.getStats();
        if (isActive) {
          setStats(response);
        }
      } catch {
        if (isActive) {
          setStats(null);
        }
      }
    };

    load();

    return () => {
      isActive = false;
    };
  }, []);

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-semibold">Командний центр</h1>
        <p className="mt-2 text-sm text-slate-400">
          Огляд ключових показників кіберспортивної ліги в реальному часі.
        </p>
      </div>
      <div className="grid gap-6 md:grid-cols-3">
        <StatCard
          title="Активні турніри"
          value={stats ? String(stats.activeTournaments) : "-"}
          trend={stats ? `Всього: ${stats.totalTournaments}` : "Дані оновлюються"}
          icon={<Trophy className="h-4 w-4" />}
        />
        <StatCard
          title="Завершені турніри"
          value={stats ? String(stats.completedTournaments) : "-"}
          trend={stats ? `Реєстрацій відкрито: ${stats.registrationOpen}` : "Дані оновлюються"}
          icon={<UsersRound className="h-4 w-4" />}
        />
        <StatCard
          title="Призовий фонд"
          value={stats ? `$${stats.totalPrizePool}` : "-"}
          trend="Сумарно по турнірах"
          icon={<CalendarClock className="h-4 w-4" />}
        />
      </div>
      <div className="glass-panel rounded-2xl p-6">
        <h2 className="text-xl font-semibold">Оперативні сповіщення</h2>
        <ul className="mt-4 space-y-3 text-sm text-slate-300">
          <li>Новий турнір "Cyber Clash" очікує підтвердження пулу команд.</li>
          <li>Матч "Nova vs Titan" перенесено на 20:30.</li>
          <li>Команда "Spectral" запросила зміну складу.</li>
        </ul>
      </div>
    </div>
  );
};

export default DashboardPage;
