import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { teamsApi } from "../../api/teamsApi";
import type { TeamDto } from "../../types";
import { useAuthStore } from "../../store/authStore";

const TeamsList = () => {
  const [teams, setTeams] = useState<TeamDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const { user, isAuthenticated } = useAuthStore();
  const canCreateTeam = user?.role === "Admin" || user?.role === "Organizer";

  useEffect(() => {
    let isActive = true;
    const load = async () => {
      try {
        const response = await teamsApi.getPaged({ page: 1, pageSize: 20, search });
        if (isActive) {
          setTeams(response.data);
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
    const approved = window.confirm("Видалити команду?");
    if (!approved) {
      return;
    }
    await teamsApi.remove(id);
    setTeams((prev) => prev.filter((team) => team.id !== id));
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold">Команди</h1>
          <p className="mt-2 text-sm text-slate-400">Огляд складів та командного рейтингу.</p>
        </div>
        {isAuthenticated && canCreateTeam && (
          <Link
            to="/teams/new"
            className="rounded-xl border border-neon-cyan/40 px-4 py-2 text-sm text-neon-cyan hover:bg-neon-cyan/10"
          >
            Створити команду
          </Link>
        )}
      </header>
      <div className="max-w-md">
        <input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Пошук за назвою"
          className="w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm text-slate-200"
        />
      </div>
      <div className="glass-panel rounded-2xl p-6">
        {loading && <div className="text-sm text-slate-400">Завантаження команд...</div>}
        <table className="w-full text-left text-sm">
          <thead className="text-xs uppercase text-slate-500">
            <tr>
              <th className="py-2">Команда</th>
              <th className="py-2">Тег</th>
              <th className="py-2">Регіон</th>
              <th className="py-2">Капітан</th>
              <th className="py-2"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/5">
            {teams.map((team) => (
              <tr key={team.id}>
                <td className="py-3 font-semibold text-white">{team.name}</td>
                <td className="py-3 text-slate-300">{team.tag}</td>
                <td className="py-3 text-slate-300">{team.region}</td>
                <td className="py-3 text-neon-cyan">
                  {team.captain ? team.captain.username : "Не призначено"}
                </td>
                <td className="py-3 text-right">
                  <div className="flex items-center justify-end gap-3">
                    <Link to={`/teams/${team.id}`} className="text-neon-cyan">
                      Деталі
                    </Link>
                    {user?.id === team.captain?.id && (
                      <>
                        <Link to={`/teams/${team.id}/edit`} className="text-slate-300">
                          Редагувати
                        </Link>
                        <button
                          type="button"
                          onClick={() => handleDelete(team.id)}
                          className="text-neon-magenta"
                        >
                          Видалити
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
    </div>
  );
};

export default TeamsList;
