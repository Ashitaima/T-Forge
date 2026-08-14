import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Trophy } from "lucide-react";
import { standingsApi } from "../../api/standingsApi";
import { EmptyState, PageHeader, Skeleton } from "../../components/ui/Primitives";
import type { PlayerStandingDto, TeamStandingDto } from "../../types";

type Tab = "teams" | "players";

/** Перші три місця позначені кольором — далі рейтинг читається просто як число. */
const RankCell = ({ rank }: { rank: number }) => (
  <span
    className={`tabular font-mono ${
      rank === 1 ? "text-ember" : rank <= 3 ? "text-text" : "text-text-faint"
    }`}
  >
    {String(rank).padStart(2, "0")}
  </span>
);

const StandingsPage = () => {
  const [tab, setTab] = useState<Tab>("teams");
  const [teams, setTeams] = useState<TeamStandingDto[]>([]);
  const [players, setPlayers] = useState<PlayerStandingDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isActive = true;

    Promise.all([
      standingsApi.getTeams().catch(() => [] as TeamStandingDto[]),
      standingsApi.getPlayers().catch(() => [] as PlayerStandingDto[])
    ])
      .then(([teamRows, playerRows]) => {
        if (!isActive) {
          return;
        }
        setTeams(teamRows);
        setPlayers(playerRows);
      })
      .finally(() => isActive && setLoading(false));

    return () => {
      isActive = false;
    };
  }, []);

  const tabs: { id: Tab; label: string; count: number }[] = [
    { id: "teams", label: "Команди", count: teams.length },
    { id: "players", label: "Гравці", count: players.length }
  ];

  return (
    <>
      <PageHeader
        eyebrow="Рейтинг"
        title="Таблиця"
        description="Підсумки всіх зіграних матчів: команди за титулами й перемогами, гравці за KDA."
      />

      <div className="flex gap-1 border-b border-line-soft">
        {tabs.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setTab(item.id)}
            className={`-mb-px border-b-2 px-4 py-2.5 text-body transition ${
              tab === item.id
                ? "border-ember text-text"
                : "border-transparent text-text-muted hover:text-text"
            }`}
          >
            {item.label}
            <span className="tabular ml-2 font-mono text-micro text-text-faint">{item.count}</span>
          </button>
        ))}
      </div>

      {loading && <Skeleton rows={5} />}

      {!loading && tab === "teams" && (
        teams.length === 0 ? (
          <EmptyState
            title="Ще немає зіграних матчів"
            hint="Таблиця заповниться, щойно завершиться перший матч турніру."
          />
        ) : (
          <div className="panel overflow-x-auto">
            <table className="table">
              <thead>
                <tr>
                  <th className="w-px">#</th>
                  <th>Команда</th>
                  <th className="text-right">Ігор</th>
                  <th className="text-right">В</th>
                  <th className="text-right">П</th>
                  <th className="text-right">%</th>
                  <th className="text-right">Титули</th>
                </tr>
              </thead>
              <tbody>
                {teams.map((row) => (
                  <tr key={row.team?.id}>
                    <td><RankCell rank={row.rank} /></td>
                    <td className="cell-primary">
                      <Link to={`/teams/${row.team?.id}`} className="hover:text-ember">
                        {row.team?.name}
                      </Link>
                      <span className="ml-2 font-mono text-micro font-normal text-text-faint">
                        {row.team?.tag}
                      </span>
                    </td>
                    <td className="tabular text-right font-mono">{row.played}</td>
                    <td className="tabular text-right font-mono text-win">{row.wins}</td>
                    <td className="tabular text-right font-mono">{row.losses}</td>
                    <td className="tabular text-right font-mono">{row.winRate}%</td>
                    <td className="text-right">
                      {row.titles > 0 ? (
                        <span className="inline-flex items-center gap-1 text-ember">
                          <Trophy className="h-3.5 w-3.5" />
                          <span className="tabular font-mono">{row.titles}</span>
                        </span>
                      ) : (
                        <span className="tabular font-mono text-text-faint">0</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )
      )}

      {!loading && tab === "players" && (
        players.length === 0 ? (
          <EmptyState
            title="Статистики гравців ще немає"
            hint="Внесіть ростер матчу та показники вбивств, смертей і асистів — рейтинг збереться автоматично."
          />
        ) : (
          <div className="panel overflow-x-auto">
            <table className="table">
              <thead>
                <tr>
                  <th className="w-px">#</th>
                  <th>Гравець</th>
                  <th>Команда</th>
                  <th className="text-right">Матчів</th>
                  <th className="text-right">В</th>
                  <th className="text-right">С</th>
                  <th className="text-right">А</th>
                  <th className="text-right">KDA</th>
                </tr>
              </thead>
              <tbody>
                {players.map((row) => (
                  <tr key={row.player?.id}>
                    <td><RankCell rank={row.rank} /></td>
                    <td className="cell-primary">{row.player?.nickname}</td>
                    <td>{row.teamName ?? <span className="text-text-faint">Вільний агент</span>}</td>
                    <td className="tabular text-right font-mono">{row.matches}</td>
                    <td className="tabular text-right font-mono">{row.kills}</td>
                    <td className="tabular text-right font-mono">{row.deaths}</td>
                    <td className="tabular text-right font-mono">{row.assists}</td>
                    <td className="tabular text-right font-mono text-text">{row.kda.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )
      )}
    </>
  );
};

export default StandingsPage;
