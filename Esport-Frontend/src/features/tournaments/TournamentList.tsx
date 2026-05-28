import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { tournamentsApi } from "../../api/tournamentsApi";
import type { TournamentDto } from "../../types";
import { CalendarRange, PlusCircle } from "lucide-react";
import { useAuthStore } from "../../store/authStore";

const TournamentList = () => {
  const [data, setData] = useState<TournamentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { user } = useAuthStore();
  const isOrganizer = user?.role === "Organizer";

  useEffect(() => {
    let isActive = true;
    const load = async () => {
      try {
        const response = await tournamentsApi.getAllActive();
        if (isActive) {
          setData(response);
        }
      } catch (err) {
        if (isActive) {
          setError("Не вдалося отримати список турнірів");
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

  const stats = useMemo(() => {
    const active = data.filter((tournament) => tournament.status === "InProgress").length;
    return { total: data.length, active };
  }, [data]);

  const statusLabel = (status: string) => {
    switch (status) {
      case "InProgress":
        return "У процесі";
      case "Completed":
        return "Завершено";
      case "Planned":
        return "Заплановано";
      default:
        return status;
    }
  };

  const handleDelete = async (id: number) => {
    const approved = window.confirm("Ви дійсно хочете видалити турнір?");
    if (!approved) {
      return;
    }
    await tournamentsApi.remove(id);
    setData((prev) => prev.filter((tournament) => tournament.id !== id));
  };

  return (
    <div className="space-y-8">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold">Турніри</h1>
          <p className="mt-2 text-sm text-slate-400">
            Керуйте активними лігами, форматом матчів та призовими фондами.
          </p>
        </div>
        {isOrganizer && (
          <Link
            to="/tournaments/new"
            className="inline-flex items-center gap-2 rounded-xl border border-neon-cyan/40 px-4 py-2 text-sm text-neon-cyan hover:bg-neon-cyan/10"
          >
            <PlusCircle className="h-4 w-4" />
            Створити турнір
          </Link>
        )}
      </header>

      <section className="grid gap-6 md:grid-cols-2">
        <div className="glass-panel rounded-2xl p-5">
          <div className="text-sm text-slate-400">Усього турнірів</div>
          <div className="mt-2 text-2xl font-semibold text-white">{stats.total}</div>
        </div>
        <div className="glass-panel rounded-2xl p-5">
          <div className="text-sm text-slate-400">Активні зараз</div>
          <div className="mt-2 text-2xl font-semibold text-neon-green">{stats.active}</div>
        </div>
      </section>

      {loading && <div className="text-sm text-slate-400">Завантаження турнірів...</div>}
      {error && <div className="text-sm text-neon-magenta">{error}</div>}

      {!loading && !error && (
        <div className="grid gap-6 lg:grid-cols-3">
          {data.map((tournament) => (
            <article key={tournament.id} className="glass-panel rounded-2xl p-5">
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold text-white">{tournament.name}</h3>
                <span className="rounded-full border border-neon-cyan/40 px-2 py-1 text-xs text-neon-cyan">
                  {statusLabel(tournament.status)}
                </span>
              </div>
              <div className="mt-2 text-sm text-slate-400">{tournament.game}</div>
              <p className="mt-3 text-sm text-slate-300">{tournament.description}</p>
              <div className="mt-4 flex items-center gap-2 text-sm text-slate-300">
                <CalendarRange className="h-4 w-4 text-neon-green" />
                {new Date(tournament.startDate).toLocaleDateString("uk-UA")} -
                {" "}
                {new Date(tournament.endDate).toLocaleDateString("uk-UA")}
              </div>
              <div className="mt-3 text-xs text-slate-400">
                Команд: {tournament.currentTeams}/{tournament.maxTeams}
              </div>
              <div className="mt-3 text-sm text-slate-400">Призовий фонд</div>
              <div className="text-xl font-semibold text-neon-magenta">${tournament.prizePool}</div>
              <div className="mt-5 flex items-center justify-between">
                <Link to={`/tournaments/${tournament.id}`} className="text-sm text-neon-cyan">
                  Переглянути деталі
                </Link>
                {isOrganizer && (
                  <div className="flex items-center gap-3 text-sm">
                    <Link to={`/tournaments/${tournament.id}/edit`} className="text-slate-300">
                      Редагувати
                    </Link>
                    <button
                      type="button"
                      onClick={() => handleDelete(tournament.id)}
                      className="text-neon-magenta"
                    >
                      Видалити
                    </button>
                  </div>
                )}
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
};

export default TournamentList;
