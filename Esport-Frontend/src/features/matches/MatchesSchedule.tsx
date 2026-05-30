import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { matchesApi } from "../../api/matchesApi";
import type { MatchDto } from "../../types";
import { useAuthStore } from "../../store/authStore";

const MatchesSchedule = () => {
  const [scheduledMatches, setScheduledMatches] = useState<MatchDto[]>([]);
  const [completedMatches, setCompletedMatches] = useState<MatchDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const { user } = useAuthStore();
  const isOrganizer = user?.role === "Organizer";

  useEffect(() => {
    let isActive = true;
    const load = async () => {
      try {
        const [scheduled, completed] = await Promise.all([
          matchesApi.getScheduled(),
          matchesApi.getCompleted()
        ]);
        if (isActive) {
          setScheduledMatches(scheduled);
          setCompletedMatches(completed);
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
    setScheduledMatches((prev) => prev.filter((match) => match.id !== id));
    setCompletedMatches((prev) => prev.filter((match) => match.id !== id));
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

  const normalizedSearch = search.trim().toLowerCase();
  const matchFilter = (match: MatchDto) => {
    if (!normalizedSearch) {
      return true;
    }

    const haystack = [
      match.homeTeam?.name,
      match.awayTeam?.name,
      match.matchType,
      match.format,
      statusLabel(match.status)
    ]
      .filter(Boolean)
      .join(" ")
      .toLowerCase();

    return haystack.includes(normalizedSearch);
  };

  const filteredScheduledMatches = useMemo(
    () => scheduledMatches.filter(matchFilter),
    [scheduledMatches, normalizedSearch]
  );

  const filteredCompletedMatches = useMemo(
    () => completedMatches.filter(matchFilter),
    [completedMatches, normalizedSearch]
  );

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
      <div className="max-w-md">
        <input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Пошук за командами, форматом або статусом"
          className="w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm text-slate-200"
        />
      </div>
      {loading && <div className="text-sm text-slate-400">Завантаження матчів...</div>}
      {!loading && (
        <div className="space-y-8">
          <section className="space-y-4">
            <h2 className="text-xl font-semibold">Заплановані матчі</h2>
            {filteredScheduledMatches.length === 0 && (
              <div className="text-sm text-slate-400">Немає запланованих матчів.</div>
            )}
            <div className="grid gap-4">
              {filteredScheduledMatches.map((match) => (
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
          </section>

          <section className="space-y-4">
            <h2 className="text-xl font-semibold">Завершені матчі</h2>
            {filteredCompletedMatches.length === 0 && (
              <div className="text-sm text-slate-400">Немає завершених матчів.</div>
            )}
            <div className="grid gap-4">
              {filteredCompletedMatches.map((match) => (
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
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </section>
        </div>
      )}
    </div>
  );
};

export default MatchesSchedule;
