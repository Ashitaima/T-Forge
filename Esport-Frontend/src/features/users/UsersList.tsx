import { useState } from "react";
import { Link } from "react-router-dom";
import { PlusCircle, Trash2 } from "lucide-react";
import { usersApi } from "../../api/usersApi";
import { EmptyState, PageHeader, Pager, SearchField, Skeleton } from "../../components/ui/Primitives";
import { usePagedList } from "../../hooks/usePagedList";
import type { UserDto } from "../../types";

const roleLabels: Record<string, string> = {
  Admin: "Адміністратор",
  Organizer: "Організатор",
  Player: "Гравець"
};

const UsersList = () => {
  const [search, setSearch] = useState("");
  const pageSize = 20;

  const {
    items: users,
    page,
    setPage,
    totalCount,
    totalPages,
    loading,
    reload
  } = usePagedList<UserDto>(
    (page, pageSize) => usersApi.getPaged({ page, pageSize, search }),
    search,
    pageSize
  );

  const handleDelete = async (id: number) => {
    if (!window.confirm("Видалити користувача? Дію не можна скасувати.")) {
      return;
    }
    await usersApi.remove(id);
    reload();
  };

  return (
    <>
      <PageHeader
        eyebrow="Адміністрування"
        title="Користувачі"
        description="Облікові записи, ролі та доступ до системи."
        action={
          <Link to="/users/new" className="btn btn-primary">
            <PlusCircle className="h-4 w-4" />
            Додати користувача
          </Link>
        }
      />

      <SearchField value={search} onChange={setSearch} placeholder="Пошук за нікнеймом або поштою" />

      {loading && <Skeleton rows={4} />}

      {!loading && users.length === 0 && (
        <EmptyState
          title={search ? "Користувачів не знайдено" : "Список порожній"}
          hint={search ? "Спробуйте інший запит." : undefined}
        />
      )}

      {!loading && users.length > 0 && (
        <div className="panel overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>Користувач</th>
                <th>Пошта</th>
                <th>Роль</th>
                <th>Стан</th>
                <th className="w-px" />
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id}>
                  <td className="cell-primary">
                    {user.firstName} {user.lastName}
                    <div className="font-mono text-micro font-normal text-text-faint">@{user.username}</div>
                  </td>
                  <td>{user.email}</td>
                  <td>{roleLabels[user.role] ?? user.role}</td>
                  <td>
                    <span className={`pill ${user.isActive ? "pill-done" : "pill-off"}`}>
                      {user.isActive ? "Активний" : "Вимкнений"}
                    </span>
                  </td>
                  <td>
                    <div className="row-actions">
                      <Link to={`/users/${user.id}/edit`} className="btn btn-ghost btn-sm">
                        Редагувати
                      </Link>
                      <button
                        type="button"
                        onClick={() => handleDelete(user.id)}
                        className="btn btn-ghost btn-sm px-2 text-text-faint hover:text-danger"
                        aria-label={`Видалити ${user.username}`}
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
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

export default UsersList;
