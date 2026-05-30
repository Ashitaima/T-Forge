import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { playersApi } from "../../api/playersApi";
import { useAuthStore } from "../../store/authStore";
import type { PlayerDto } from "../../types";

const PlayersList = () => {
  const [players, setPlayers] = useState<PlayerDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const { isAuthenticated } = useAuthStore();

  useEffect(() => {
    let isActive = true;
    const load = async () => {
      try {
        const response = await playersApi.getPaged({ page: 1, pageSize: 20, search });
        if (isActive) {
          setPlayers(response.data);
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
    const approved = window.confirm("Видалити гравця?");
    if (!approved) {
      return;
    }
    await playersApi.remove(id);
    setPlayers((prev) => prev.filter((player) => player.id !== id));
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold">Гравці</h1>
          <p className="mt-2 text-sm text-slate-400">Каталог гравців та статистика результатів.</p>
        </div>
        {isAuthenticated && (
          <Link
            to="/players/new"
            className="rounded-xl border border-neon-cyan/40 px-4 py-2 text-sm text-neon-cyan hover:bg-neon-cyan/10"
          >
            Додати гравця
          </Link>
        )}
      </header>
      <div className="max-w-md">
        <input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Пошук за нікнеймом"
          className="w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm text-slate-200"
        />
      </div>
      <div className="glass-panel rounded-2xl p-6">
        {loading && <div className="text-sm text-slate-400">Завантаження гравців...</div>}
        <table className="w-full text-left text-sm">
          <thead className="text-xs uppercase text-slate-500">
            <tr>
              <th className="py-2">Нікнейм</th>
              <th className="py-2">Позиція</th>
              <th className="py-2">Країна</th>
              <th className="py-2">Команда</th>
              <th className="py-2"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/5">
            {players.map((player) => (
              <tr key={player.id}>
                <td className="py-3 font-semibold text-white">{player.nickname}</td>
                <td className="py-3 text-slate-300">{player.position}</td>
                <td className="py-3 text-slate-300">{player.country}</td>
                <td className="py-3 text-neon-cyan">{player.team?.name ?? "Вільний агент"}</td>
                <td className="py-3 text-right">
                  <div className="flex items-center justify-end gap-3">
                    <Link to={`/players/${player.id}/edit`} className="text-neon-cyan">
                      Редагувати
                    </Link>
                    <button
                      type="button"
                      onClick={() => handleDelete(player.id)}
                      className="text-neon-magenta"
                    >
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

export default PlayersList;
