import { useState } from "react";
import { Link } from "react-router-dom";
import { PlusCircle, Trash2 } from "lucide-react";
import { teamsApi } from "../../api/teamsApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Pager, SearchField, Skeleton } from "../../components/ui/Primitives";
import { usePagedList } from "../../hooks/usePagedList";
import type { TeamDto } from "../../types";

const TeamsList = () => {
  const [search, setSearch] = useState("");
  const { user } = useAuthStore();
  const canCreate = user?.role === "Admin" || user?.role === "Organizer";
  const pageSize = 20;

  const {
    items: teams,
    page,
    setPage,
    totalCount,
    totalPages,
    loading,
    reload
  } = usePagedList<TeamDto>(
    (page, pageSize) => teamsApi.getPaged({ page, pageSize, search }),
    search,
    pageSize
  );

  const handleDelete = async (id: number) => {
    if (!window.confirm("Видалити команду? Дію не можна скасувати.")) {
      return;
    }
    await teamsApi.remove(id);
    reload();
  };

  return (
    <>
      <PageHeader
        eyebrow="Склади"
        title="Команди"
        description="Усі зареєстровані команди, їхні капітани та регіони."
        action={
          canCreate && (
            <Link to="/teams/new" className="btn btn-primary">
              <PlusCircle className="h-4 w-4" />
              Створити команду
            </Link>
          )
        }
      />

      <SearchField value={search} onChange={setSearch} placeholder="Пошук за назвою команди" />

      {loading && <Skeleton rows={4} />}

      {!loading && teams.length === 0 && (
        <EmptyState
          title={search ? "Команд не знайдено" : "Команд ще немає"}
          hint={search ? "Спробуйте іншу назву." : "Створіть команду, щоб реєструвати її на турніри."}
          action={
            canCreate && !search ? (
              <Link to="/teams/new" className="btn btn-primary">
                Створити команду
              </Link>
            ) : undefined
          }
        />
      )}

      {!loading && teams.length > 0 && (
        <div className="panel overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>Команда</th>
                <th>Тег</th>
                <th>Регіон</th>
                <th>Капітан</th>
                <th className="w-px" />
              </tr>
            </thead>
            <tbody>
              {teams.map((team) => (
                <tr key={team.id}>
                  <td className="cell-primary">
                    <Link to={`/teams/${team.id}`} className="hover:text-ember">
                      {team.name}
                    </Link>
                  </td>
                  <td className="font-mono text-micro">{team.tag}</td>
                  <td>{team.region || "—"}</td>
                  <td>{team.captain ? `@${team.captain.username}` : "Не призначено"}</td>
                  <td>
                    <div className="row-actions">
                      <Link to={`/teams/${team.id}`} className="btn btn-ghost btn-sm">
                        Деталі
                      </Link>
                      {user?.id === team.captain?.id && (
                        <>
                          <Link to={`/teams/${team.id}/edit`} className="btn btn-ghost btn-sm">
                            Редагувати
                          </Link>
                          <button
                            type="button"
                            onClick={() => handleDelete(team.id)}
                            className="btn btn-ghost btn-sm px-2 text-text-faint hover:text-danger"
                            aria-label={`Видалити ${team.name}`}
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Pager
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        totalPages={totalPages}
        onChange={setPage}
        disabled={loading}
      />
    </>
  );
};

export default TeamsList;
