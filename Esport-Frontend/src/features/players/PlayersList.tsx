import { useState } from "react";
import { Link } from "react-router-dom";
import { PlusCircle, Trash2 } from "lucide-react";
import { playersApi } from "../../api/playersApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Pager, SearchField, Skeleton } from "../../components/ui/Primitives";
import { usePagedList } from "../../hooks/usePagedList";
import type { PlayerDto } from "../../types";

const PlayersList = () => {
  const [search, setSearch] = useState("");
  const { user, isAuthenticated } = useAuthStore();
  const pageSize = 20;

  const {
    items: players,
    page,
    setPage,
    totalCount,
    totalPages,
    loading,
    reload
  } = usePagedList<PlayerDto>(
    (page, pageSize) => playersApi.getPaged({ page, pageSize, search }),
    search,
    pageSize
  );

  const handleDelete = async (id: number) => {
    if (!window.confirm("Видалити гравця? Дію не можна скасувати.")) {
      return;
    }
    await playersApi.remove(id);
    reload();
  };

  return (
    <>
      <PageHeader
        eyebrow="Ростер"
        title="Гравці"
        description="Каталог гравців, їхні позиції та поточні команди."
        action={
          isAuthenticated && (
            <Link to="/players/new" className="btn btn-primary">
              <PlusCircle className="h-4 w-4" />
              Додати гравця
            </Link>
          )
        }
      />

      <SearchField value={search} onChange={setSearch} placeholder="Пошук за нікнеймом" />

      {loading && <Skeleton rows={4} />}

      {!loading && players.length === 0 && (
        <EmptyState
          title={search ? "Гравців не знайдено" : "Гравців ще немає"}
          hint={search ? "Спробуйте інший нікнейм." : "Створіть профіль гравця, щоб приєднатись до команди."}
        />
      )}

      {!loading && players.length > 0 && (
        <div className="panel overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>Нікнейм</th>
                <th>Позиція</th>
                <th>Країна</th>
                <th>Команда</th>
                <th className="text-right">Матчі</th>
                <th className="text-right">Перемоги</th>
                <th className="w-px" />
              </tr>
            </thead>
            <tbody>
              {players.map((player) => {
                const canEdit = user?.role === "Admin" || user?.id === player.userId;

                return (
                  <tr key={player.id}>
                    <td className="cell-primary">
                      <Link to={`/players/${player.id}`} className="hover:text-ember">
                        {player.nickname}
                      </Link>
                    </td>
                    <td>{player.position || "—"}</td>
                    <td>{player.country || "—"}</td>
                    <td>
                      {player.team ? (
                        <Link to={`/teams/${player.team.id}`} className="hover:text-ember">
                          {player.team.name}
                        </Link>
                      ) : (
                        <span className="text-text-faint">Вільний агент</span>
                      )}
                    </td>
                    <td className="tabular text-right font-mono text-micro">{player.totalMatches}</td>
                    <td className="tabular text-right font-mono text-micro">{player.wins}</td>
                    <td>
                      {canEdit && (
                        <div className="row-actions">
                          <Link to={`/players/${player.id}/edit`} className="btn btn-ghost btn-sm">
                            Редагувати
                          </Link>
                          <button
                            type="button"
                            onClick={() => handleDelete(player.id)}
                            className="btn btn-ghost btn-sm px-2 text-text-faint hover:text-danger"
                            aria-label={`Видалити ${player.nickname}`}
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })}
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

export default PlayersList;
