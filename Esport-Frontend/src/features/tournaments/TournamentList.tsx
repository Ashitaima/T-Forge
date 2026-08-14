import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { PlusCircle, Trash2 } from "lucide-react";
import { tournamentsApi } from "../../api/tournamentsApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Skeleton, StatusPill } from "../../components/ui/Primitives";
import type { TournamentDto } from "../../types";

const TournamentList = () => {
  const [data, setData] = useState<TournamentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { user } = useAuthStore();
  const canCreate = user?.role === "Organizer" || user?.role === "Admin";

  useEffect(() => {
    let isActive = true;

    tournamentsApi
      .getAllActive()
      .then((response) => isActive && setData(response))
      .catch(() => isActive && setError("Не вдалося завантажити турніри. Перевірте зʼєднання з сервером."))
      .finally(() => isActive && setLoading(false));

    return () => {
      isActive = false;
    };
  }, []);

  const handleDelete = async (id: number) => {
    if (!window.confirm("Видалити турнір? Дію не можна скасувати.")) {
      return;
    }
    await tournamentsApi.remove(id);
    setData((prev) => prev.filter((tournament) => tournament.id !== id));
  };

  return (
    <>
      <PageHeader
        eyebrow="Змагання"
        title="Турніри"
        description="Реєстрація команд, турнірні сітки та призові фонди — усе в одному місці."
        action={
          canCreate && (
            <Link to="/tournaments/new" className="btn btn-primary">
              <PlusCircle className="h-4 w-4" />
              Створити турнір
            </Link>
          )
        }
      />

      {loading && <Skeleton rows={3} />}
      {error && <div className="notice notice-error">{error}</div>}

      {!loading && !error && data.length === 0 && (
        <EmptyState
          title="Турнірів поки немає"
          hint="Створіть перший турнір, відкрийте реєстрацію команд і згенеруйте сітку."
          action={
            canCreate ? (
              <Link to="/tournaments/new" className="btn btn-primary">
                Створити турнір
              </Link>
            ) : undefined
          }
        />
      )}

      {!loading && !error && data.length > 0 && (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {data.map((tournament) => {
            const filled = tournament.maxTeams
              ? Math.min(100, Math.round((tournament.currentTeams / tournament.maxTeams) * 100))
              : 0;

            return (
              <article key={tournament.id} className="card-link group flex flex-col">
                <Link to={`/tournaments/${tournament.id}`} className="flex-1 p-5">
                  <div className="flex items-start justify-between gap-3">
                    <span className="eyebrow">{tournament.game}</span>
                    <StatusPill status={tournament.status} />
                  </div>

                  <h3 className="mt-3 font-display text-h3 leading-snug text-text">{tournament.name}</h3>
                  <p className="muted mt-2 line-clamp-2 text-body">{tournament.description}</p>

                  {/* Заповненість сітки — головна метрика на етапі реєстрації */}
                  <div className="mt-5">
                    <div className="flex items-baseline justify-between">
                      <span className="text-micro text-text-faint">Заявки команд</span>
                      <span className="tabular font-mono text-micro text-text">
                        {tournament.currentTeams}/{tournament.maxTeams}
                      </span>
                    </div>
                    <div className="mt-1.5 h-1 overflow-hidden rounded-full bg-ink-700">
                      <div
                        className="h-full rounded-full bg-ember transition-[width] duration-500"
                        style={{ width: `${filled}%` }}
                      />
                    </div>
                  </div>

                  <div className="mt-4 flex items-end justify-between border-t border-line-soft pt-4">
                    <div>
                      <div className="text-micro text-text-faint">Призовий фонд</div>
                      <div className="tabular font-mono text-lead text-text">${tournament.prizePool}</div>
                    </div>
                    <div className="tabular text-right font-mono text-micro text-text-faint">
                      {new Date(tournament.startDate).toLocaleDateString("uk-UA")}
                    </div>
                  </div>
                </Link>

                {canCreate && (
                  <div className="flex items-center justify-end gap-1 border-t border-line-soft px-3 py-2">
                    <Link to={`/tournaments/${tournament.id}/edit`} className="btn btn-ghost btn-sm">
                      Редагувати
                    </Link>
                    <button
                      type="button"
                      onClick={() => handleDelete(tournament.id)}
                      className="btn btn-ghost btn-sm px-2 text-text-faint hover:text-danger"
                      aria-label={`Видалити ${tournament.name}`}
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                )}
              </article>
            );
          })}
        </div>
      )}
    </>
  );
};

export default TournamentList;
