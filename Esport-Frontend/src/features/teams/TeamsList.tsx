import { useState } from "react";
import { Link } from "react-router-dom";
import { PlusCircle, Trash2 } from "lucide-react";
import { teamsApi } from "../../api/teamsApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Pager, SearchField, Skeleton } from "../../components/ui/Primitives";
import { SortableTh, useSortState } from "../../components/ui/SortableTh";
import { RatingCell } from "../../components/ui/Rating";
import { usePagedList } from "../../hooks/usePagedList";
import type { TeamRowDto } from "../../types";

const TeamsList = () => {
  const [search, setSearch] = useState("");
  const { user, isAuthenticated } = useAuthStore();
  // Команду може створити будь-який авторизований користувач — він стає її
  // капітаном (id власника сервер бере з токена).
  const canCreate = isAuthenticated;
  const pageSize = 20;

  const { sortBy, sortDirection, onSort } = useSortState("titles");

  const {
    items: teams,
    page,
    setPage,
    totalCount,
    totalPages,
    loading,
    reload
  } = usePagedList<TeamRowDto>(
    (page, pageSize) => teamsApi.getPaged({ page, pageSize, search, sortBy, sortDirection }),
    // Кожне значення, яке замикає в собі запит, має бути в ключі.
    `${search}|${sortBy}|${sortDirection}`,
    pageSize
  );

  const handleDelete = async (id: number) => {
    if (!window.confirm("Видалити команду? Дію не можна скасувати.")) {
      return;
    }
    await teamsApi.remove(id);
    reload();
  };

  const sortProps = { activeKey: sortBy, direction: sortDirection, onSort };

  return (
    <>
      <PageHeader
        eyebrow="Склади"
        title="Команди"
        description="Усі зареєстровані команди, їхні капітани та підсумки за зіграними матчами."
        action={
          canCreate && (
            <Link to="/teams/new" className="btn btn-primary">
              <PlusCircle className="h-4 w-4" />
              Створити команду
            </Link>
          )
        }
      />

      <SearchField value={search} onChange={setSearch} placeholder="Пошук за назвою або тегом" />

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
                <th className="w-px text-right">#</th>
                <SortableTh label="Команда" sortKey="name" {...sortProps} />
                <SortableTh label="Тег" sortKey="tag" {...sortProps} />
                <SortableTh label="Регіон" sortKey="region" {...sortProps} />
                <th>Капітан</th>
                <SortableTh label="Зіграно" sortKey="played" align="right" {...sortProps} />
                <SortableTh label="Перемоги" sortKey="wins" align="right" {...sortProps} />
                <SortableTh label="%" sortKey="winRate" align="right" {...sortProps} />
                <SortableTh label="Титули" sortKey="titles" align="right" {...sortProps} />
                <SortableTh label="Рейтинг" sortKey="rating" align="right" {...sortProps} />
                <th className="w-px" />
              </tr>
            </thead>
            <tbody>
              {teams.map((team, index) => (
                <tr key={team.id}>
                  {/* Порядковий номер у поточному сортуванні, а не збережений ранг */}
                  <td className="tabular text-right font-mono text-micro text-text-faint">
                    {(page - 1) * pageSize + index + 1}
                  </td>
                  <td className="cell-primary">
                    <Link to={`/teams/${team.id}`} className="hover:text-ember">
                      {team.name}
                    </Link>
                  </td>
                  <td className="font-mono text-micro">{team.tag}</td>
                  <td>{team.region || "—"}</td>
                  <td>{team.captainUsername ? `@${team.captainUsername}` : "Не призначено"}</td>
                  <td className="tabular text-right font-mono text-micro">{team.played}</td>
                  <td className="tabular text-right font-mono text-micro">{team.wins}</td>
                  <td className="tabular text-right font-mono text-micro">{team.winRate}</td>
                  <td className="tabular text-right font-mono text-micro">
                    {team.titles > 0 ? <span className="text-ember">{team.titles}</span> : team.titles}
                  </td>
                  <td className="text-right text-micro">
                    <RatingCell rating={team.rating} game={team.ratingGame} />
                  </td>
                  <td>
                    <div className="row-actions">
                      <Link to={`/teams/${team.id}`} className="btn btn-ghost btn-sm">
                        Деталі
                      </Link>
                      {user?.id === team.captainId && (
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
