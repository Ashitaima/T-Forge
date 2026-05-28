import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { matchesApi } from "../../api/matchesApi";
import { tournamentsApi } from "../../api/tournamentsApi";
import type { MatchDto, TournamentDto } from "../../types";

const TournamentDetail = () => {
  const { id } = useParams();
  const [tournament, setTournament] = useState<TournamentDto | null>(null);
  const [matches, setMatches] = useState<MatchDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) {
      return;
    }

    const load = async () => {
      setLoading(true);
      try {
        const [tournamentData, matchesData] = await Promise.all([
          tournamentsApi.getById(Number(id)),
          matchesApi.getPaged({ page: 1, pageSize: 8, tournamentId: Number(id) })
        ]);
        setTournament(tournamentData);
        setMatches(matchesData.data);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [id]);

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold">{tournament?.name ?? `Турнір #${id}`}</h1>
          <p className="mt-2 text-sm text-slate-400">Сітка матчів та поточний статус</p>
        </div>
        <Link to={`/tournaments/${id}/edit`} className="text-sm text-neon-cyan">
          Редагувати
        </Link>
      </header>
      <div className="glass-panel rounded-2xl p-6">
        <h2 className="text-xl font-semibold">Сітка матчів</h2>
        {loading && <div className="mt-4 text-sm text-slate-400">Завантаження матчів...</div>}
        {!loading && matches.length === 0 && (
          <div className="mt-4 text-sm text-slate-400">Поки немає матчів для цього турніру.</div>
        )}
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          {matches.map((match) => (
            <div key={match.id} className="rounded-xl border border-white/10 bg-night-800/60 p-4">
              <div className="text-sm text-slate-400">{match.matchType}</div>
              <div className="mt-2 text-sm text-slate-200">
                {match.homeTeam?.name ?? "TBD"} vs {match.awayTeam?.name ?? "TBD"}
              </div>
              <div className="text-xs text-neon-green">
                {match.format} • {new Date(match.scheduledAt).toLocaleString("uk-UA")}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default TournamentDetail;
