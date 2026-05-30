import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { usersApi } from "../../api/usersApi";
import type { UserDto } from "../../types";

const UsersList = () => {
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  useEffect(() => {
    let isActive = true;
    const load = async () => {
      try {
        const response = await usersApi.getPaged({ page: 1, pageSize: 20, search });
        if (isActive) {
          setUsers(response.data);
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
  }, [search]);

  const handleDelete = async (id: number) => {
    const approved = window.confirm("Видалити користувача?");
    if (!approved) {
      return;
    }
    await usersApi.remove(id);
    setUsers((prev) => prev.filter((user) => user.id !== id));
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold">Користувачі</h1>
          <p className="mt-2 text-sm text-slate-400">Адміністрування ролей та доступу.</p>
        </div>
        <Link
          to="/users/new"
          className="rounded-xl border border-neon-cyan/40 px-4 py-2 text-sm text-neon-cyan hover:bg-neon-cyan/10"
        >
          Додати користувача
        </Link>
      </header>
      <div className="max-w-md">
        <input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Пошук за нікнеймом або email"
          className="w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm text-slate-200"
        />
      </div>
      <div className="glass-panel rounded-2xl p-6">
        {loading && <div className="text-sm text-slate-400">Завантаження користувачів...</div>}
        <table className="w-full text-left text-sm">
          <thead className="text-xs uppercase text-slate-500">
            <tr>
              <th className="py-2">Нікнейм</th>
              <th className="py-2">Email</th>
              <th className="py-2">Роль</th>
              <th className="py-2"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/5">
            {users.map((user) => (
              <tr key={user.id}>
                <td className="py-3 font-semibold text-white">{user.username}</td>
                <td className="py-3 text-slate-300">{user.email}</td>
                <td className="py-3 text-neon-cyan">{user.role}</td>
                <td className="py-3 text-right">
                  <div className="flex items-center justify-end gap-3">
                    <Link to={`/users/${user.id}/edit`} className="text-neon-cyan">
                      Редагувати
                    </Link>
                    <button type="button" onClick={() => handleDelete(user.id)} className="text-neon-magenta">
                      Видалити
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default UsersList;
