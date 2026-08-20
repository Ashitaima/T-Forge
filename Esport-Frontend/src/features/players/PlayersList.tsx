import { useState } from "react";
import { Link } from "react-router-dom";
import { PlusCircle, Trash2 } from "lucide-react";
import { playersApi } from "../../api/playersApi";
import { useAuthStore } from "../../store/authStore";
import { useIsRole } from "../../hooks/useEffectiveRole";
import { EmptyState, PageHeader, Pager, SearchField, Skeleton } from "../../components/ui/Primitives";
import { SortableTh, useSortState } from "../../components/ui/SortableTh";
import { Avatar, teamInitials } from "../../components/ui/Avatar";
import { CountryFlag } from "../../components/ui/CountryFlag";
import { RatingCell } from "../../components/ui/Rating";
import { usePagedList } from "../../hooks/usePagedList";
import type { PlayerRowDto } from "../../types";

const PlayersList = () => {
  const [search, setSearch] = useState("");
  const { user } = useAuthStore();
  // Створювати профілі може лише адміністратор — решта отримує свій під час реєстрації.
  const isAdmin = useIsRole("Admin");
  const pageSize = 20;

  const { sortBy, sortDirection, onSort } = useSortState("kda");

  const {
    items: players,
    page,
    setPage,
    totalCount,
    totalPages,
    loading,
    reload
  } = usePagedList<PlayerRowDto>(
    (page, pageSize) => playersApi.getPaged({ page, pageSize, search, sortBy, sortDirection }),
    // Кожне значення, яке замикає в собі запит, має бути в ключі — інакше
    // список мовчки перестане оновлюватися при зміні сортування.
    `${search}|${sortBy}|${sortDirection}`,
    pageSize
  );

  const handleDelete = async (id: number) => {
    if (!window.confirm("Видалити гравця? Дію не можна скасувати.")) {
      return;
    }
    await playersApi.remove(id);
    reload();
  };

  const sortProps = { activeKey: sortBy, direction: sortDirection, onSort };

  return (
    <>
      <PageHeader
        eyebrow="Ростер"
        title="Гравці"
        description="Каталог гравців, їхні команди та підсумкова статистика за всіма матчами."
        action={
          isAdmin && (
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
          hint={search ? "Спробуйте інший нікнейм." : "Гравці зʼявляються тут після реєстрації."}
        />
      )}

      {!loading && players.length > 0 && (
        <div className="panel overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th className="w-px text-right">#</th>
                <SortableTh label="Нікнейм" sortKey="nickname" {...sortProps} />
                <SortableTh
                  label="Позиція"
                  sortKey="position"
                  className="hidden lg:table-cell"
                  {...sortProps}
                />
                <SortableTh
                  label="Країна"
                  sortKey="country"
                  className="hidden md:table-cell"
                  {...sortProps}
                />
                <SortableTh label="Команда" sortKey="team" {...sortProps} />
                <SortableTh
                  label="Матчі"
                  sortKey="matches"
                  align="right"
                  className="hidden xl:table-cell"
                  {...sortProps}
                />
                <SortableTh
                  label="Перемоги"
                  sortKey="wins"
                  align="right"
                  className="hidden xl:table-cell"
                  {...sortProps}
                />
                <SortableTh label="%" sortKey="winRate" align="right" {...sortProps} />
                <SortableTh label="KDA" sortKey="kda" align="right" {...sortProps} />
                <SortableTh label="Рейтинг" sortKey="rating" align="right" {...sortProps} />
                <th className="w-px" />
              </tr>
            </thead>
            <tbody>
              {players.map((player, index) => {
                const canEdit = isAdmin || user?.id === player.userId;

                return (
                  <tr key={player.id}>
                    {/* Порядковий номер у поточному сортуванні, а не збережений ранг */}
                    <td className="tabular text-right font-mono text-micro text-text-faint">
                      {(page - 1) * pageSize + index + 1}
                    </td>
                    <td className="cell-primary">
                      <Link
                        to={`/players/${player.id}`}
                        className="flex items-center gap-2.5 hover:text-ember"
                      >
                        <Avatar
                          url={player.avatarUrl}
                          fallback={player.nickname.slice(0, 2)}
                          size="sm"
                        />
                        {player.nickname}
                      </Link>
                    </td>
                    <td className="hidden lg:table-cell">{player.position || "—"}</td>
                    <td className="hidden md:table-cell">
                      <CountryFlag code={player.country} />
                    </td>
                    <td>
                      {player.teamId ? (
                        <Link
                          to={`/teams/${player.teamId}`}
                          className="flex items-center gap-2 hover:text-ember"
                        >
                          <Avatar
                            url={player.teamLogoPath}
                            fallback={teamInitials(player.teamName, player.teamTag)}
                            size="sm"
                            shape="square"
                          />
                          <span className="truncate">{player.teamName}</span>
                        </Link>
                      ) : (
                        <span className="text-text-faint">Вільний агент</span>
                      )}
                    </td>
                    <td className="tabular hidden text-right font-mono text-micro xl:table-cell">
                      {player.matches}
                    </td>
                    <td className="tabular hidden text-right font-mono text-micro xl:table-cell">
                      {player.wins}
                    </td>
                    <td className="tabular text-right font-mono text-micro">{player.winRate}</td>
                    <td className="tabular text-right font-mono text-micro">{player.kda}</td>
                    <td className="text-right text-micro">
                      <RatingCell rating={player.rating} game={player.ratingGame} />
                    </td>
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
