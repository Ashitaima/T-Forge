import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { teamsApi } from "../../api/teamsApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Skeleton } from "../../components/ui/Primitives";
import type { TeamDto } from "../../types";

const TeamDetail = () => {
  const { id } = useParams();
  const [team, setTeam] = useState<TeamDto | null>(null);
  const [loading, setLoading] = useState(true);
  const { user } = useAuthStore();

  useEffect(() => {
    if (!id) {
      return;
    }

    let isActive = true;
    setLoading(true);

    teamsApi
      .getWithPlayers(Number(id))
      .then((response) => isActive && setTeam(response))
      .finally(() => isActive && setLoading(false));

    return () => {
      isActive = false;
    };
  }, [id]);

  const isCaptain = user?.id === team?.captain?.id;

  return (
    <>
      <PageHeader
        eyebrow={team?.tag}
        title={team?.name ?? `Команда #${id}`}
        description={team?.description}
        action={
          isCaptain ? (
            <Link to={`/teams/${id}/edit`} className="btn btn-secondary">
              Редагувати
            </Link>
          ) : undefined
        }
      />

      {team && (
        <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-body text-text-muted">
          <span>
            Капітан{" "}
            <span className="text-text">{team.captain ? `@${team.captain.username}` : "не призначено"}</span>
          </span>
          {team.region && (
            <span>
              Регіон <span className="text-text">{team.region}</span>
            </span>
          )}
          <span>
            Гравців <span className="tabular font-mono text-text">{team.players?.length ?? 0}</span>
          </span>
        </div>
      )}

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Склад</h2>
        </div>
        <div className="panel-body">
          {loading && <Skeleton rows={3} />}
          {!loading && (team?.players?.length ?? 0) === 0 && (
            <EmptyState
              title="Склад порожній"
              hint="Гравці зʼявляться тут, щойно капітан додасть їх до команди."
            />
          )}
          {!loading && (team?.players?.length ?? 0) > 0 && (
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              {team?.players?.map((player) => (
                <div key={player.id} className="surface-raised px-4 py-3.5">
                  <div className="flex items-start justify-between gap-2">
                    <span className="truncate text-body font-medium text-text">{player.nickname}</span>
                    {!player.isActive && <span className="pill pill-off shrink-0">Неактивний</span>}
                  </div>
                  <div className="muted mt-1 text-micro">{player.position || "Позиція не вказана"}</div>
                  {player.country && (
                    <div className="mt-2 font-mono text-micro text-text-faint">{player.country}</div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </section>
    </>
  );
};

export default TeamDetail;
