import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { teamsApi } from "../../api/teamsApi";
import type { TeamDto } from "../../types";

const TeamDetail = () => {
  const { id } = useParams();
  const [team, setTeam] = useState<TeamDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) {
      return;
    }

    const load = async () => {
      setLoading(true);
      try {
        const response = await teamsApi.getWithPlayers(Number(id));
        setTeam(response);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [id]);

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-3xl font-semibold">{team?.name ?? `Команда #${id}`}</h1>
        <p className="mt-2 text-sm text-slate-400">Склад, ролі та активність гравців.</p>
      </header>
      <div className="glass-panel rounded-2xl p-6">
        <h2 className="text-xl font-semibold">Гравці</h2>
        {loading && <div className="mt-4 text-sm text-slate-400">Завантаження складу...</div>}
        {!loading && team?.players?.length === 0 && (
          <div className="mt-4 text-sm text-slate-400">У команди ще немає гравців.</div>
        )}
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          {team?.players?.map((player) => (
            <div key={player.id} className="rounded-xl border border-white/10 bg-night-800/60 p-4">
              <div className="text-sm text-slate-400">{player.position}</div>
              <div className="mt-2 text-lg font-semibold text-white">{player.nickname}</div>
              <div className="mt-1 text-xs text-slate-400">Країна: {player.country}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default TeamDetail;
