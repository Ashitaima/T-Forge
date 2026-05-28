import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { matchesApi } from "../../api/matchesApi";
import type { MatchDto } from "../../types";
import { useAuthStore } from "../../store/authStore";

const MatchesSchedule = () => {
  const [matches, setMatches] = useState<MatchDto[]>([]);
  const [loading, setLoading] = useState(true);
  const { user } = useAuthStore();
  const isOrganizer = user?.role === "Organizer";

  useEffect(() => {
    let isActive = true;
    const load = async () => {
      try {
        const response = await matchesApi.getScheduled();
        if (isActive) {
          setMatches(response);
        }
      } finally {
        if (isActive) {
          setLoading(false);
        }
      }
    };

    load();

    return () => {
      isActive = false;
    };
  }, []);

  const handleDelete = async (id: number) => {
    const approved = window.confirm("Видалити матч?");
    if (!approved) {
      return;
    }
    await matchesApi.remove(id);
    setMatches((prev) => prev.filter((match) => match.id !== id));
  };

  const statusLabel = (status: string) => {
    switch (status) {
      case "InProgress":
        return "У процесі";
      case "Completed":
        return "Завершено";
      case "Scheduled":
        return "Заплановано";
      default:
        return status;
    }
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold">Матчі</h1>
          <p className="mt-2 text-sm text-slate-400">Розклад та результати матчів у режимі реального часу.</p>
        </div>
        {isOrganizer && (
          <Link
            to="/matches/new"
            className="rounded-xl border border-neon-cyan/40 px-4 py-2 text-sm text-neon-cyan hover:bg-neon-cyan/10"
          >
            Додати матч
          </Link>
        )}
      </header>
      {loading && <div className="text-sm text-slate-400">Завантаження матчів...</div>}
      <div className="grid gap-4">
        {matches.map((match) => (
          <div key={match.id} className="glass-panel rounded-2xl p-5">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div>
                <div className="text-sm text-slate-400">
                  {new Date(match.scheduledAt).toLocaleString("uk-UA")}
                </div>
                <div className="mt-1 text-lg font-semibold text-white">
                  {match.homeTeam?.name ?? "TBD"} vs {match.awayTeam?.name ?? "TBD"}
                </div>
                <div className="mt-1 text-xs text-slate-400">Статус: {statusLabel(match.status)}</div>
              </div>
              <div className="text-right">
                <div className="text-xs text-slate-400">Формат</div>
                <div className="text-sm text-neon-cyan">{match.format}</div>
                <div className="mt-1 text-xs text-slate-400">{match.matchType}</div>
                {(match.homeTeamScore || match.awayTeamScore) && (
                  <div className="mt-1 text-sm text-neon-green">
                    Рахунок: {match.homeTeamScore} - {match.awayTeamScore}
                  </div>
                )}
                {isOrganizer && (
                  <div className="mt-3 flex items-center justify-end gap-3 text-xs">
                    <Link to={`/matches/${match.id}/edit`} className="text-slate-300">
                      Редагувати
                    </Link>
                    <button type="button" onClick={() => handleDelete(match.id)} className="text-neon-magenta">
                      Видалити
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default MatchesSchedule;
