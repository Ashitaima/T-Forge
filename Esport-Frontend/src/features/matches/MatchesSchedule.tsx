import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { PlusCircle, Trash2 } from "lucide-react";
import { matchesApi } from "../../api/matchesApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, SearchField, Skeleton, StatusPill } from "../../components/ui/Primitives";
import type { MatchDto } from "../../types";

const MatchRow = ({
  match,
  showScore,
  canEdit,
  onDelete
}: {
  match: MatchDto;
  showScore: boolean;
  canEdit: boolean;
  onDelete: (id: number) => void;
}) => {
  const homeWon = match.winnerTeam?.id === match.homeTeam?.id;
  const awayWon = match.winnerTeam?.id === match.awayTeam?.id;

  return (
    <li className="flex flex-wrap items-center gap-x-5 gap-y-3 py-4 first:pt-0 last:pb-0">
      <div className="tabular w-24 shrink-0 font-mono text-micro text-text-faint">
        {new Date(match.scheduledAt).toLocaleDateString("uk-UA", { day: "2-digit", month: "short" })}
        <div className="text-text-faint">
          {new Date(match.scheduledAt).toLocaleTimeString("uk-UA", { hour: "2-digit", minute: "2-digit" })}
        </div>
      </div>

      {/* Команди вирівняні по центральній осі рахунку, як у турнірній таблиці */}
      <div className="flex min-w-[15rem] flex-1 items-center gap-3">
        <span className={`flex-1 truncate text-right text-body ${homeWon ? "text-text" : "text-text-muted"}`}>
          {match.homeTeam?.name ?? "Очікується"}
        </span>
        {showScore ? (
          <span className="tabular shrink-0 rounded-md border border-line bg-ink-950 px-2.5 py-1 font-mono text-body text-text">
            {match.homeTeamScore}:{match.awayTeamScore}
          </span>
        ) : (
          <span className="shrink-0 font-mono text-micro text-text-faint">vs</span>
        )}
        <span className={`flex-1 truncate text-body ${awayWon ? "text-text" : "text-text-muted"}`}>
          {match.awayTeam?.name ?? "Очікується"}
        </span>
      </div>

      <div className="flex shrink-0 items-center gap-3">
        <span className="font-mono text-micro text-text-faint">{match.format}</span>
        <StatusPill status={match.status} />
        <Link to={`/matches/${match.id}`} className="btn btn-ghost btn-sm">
          Деталі
        </Link>
      </div>

      {canEdit && (
        <div className="row-actions ml-auto">
          <Link to={`/matches/${match.id}/edit`} className="btn btn-ghost btn-sm">
            Редагувати
          </Link>
          <button
            type="button"
            onClick={() => onDelete(match.id)}
            className="btn btn-ghost btn-sm px-2 text-text-faint hover:text-danger"
            aria-label="Видалити матч"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      )}
    </li>
  );
};

const MatchesSchedule = () => {
  const [scheduled, setScheduled] = useState<MatchDto[]>([]);
  const [completed, setCompleted] = useState<MatchDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const { user } = useAuthStore();
  const canEdit = user?.role === "Organizer" || user?.role === "Admin";

  useEffect(() => {
    let isActive = true;

    Promise.all([
      matchesApi.getScheduled().catch(() => [] as MatchDto[]),
      matchesApi.getCompleted().catch(() => [] as MatchDto[])
    ])
      .then(([scheduledData, completedData]) => {
        if (!isActive) {
          return;
        }
        setScheduled(scheduledData);
        setCompleted(completedData);
      })
      .finally(() => isActive && setLoading(false));

    return () => {
      isActive = false;
    };
  }, []);

  const handleDelete = async (id: number) => {
    if (!window.confirm("Видалити матч? Дію не можна скасувати.")) {
      return;
    }
    await matchesApi.remove(id);
    setScheduled((prev) => prev.filter((match) => match.id !== id));
    setCompleted((prev) => prev.filter((match) => match.id !== id));
  };

  const term = search.trim().toLowerCase();
  const matchesSearch = (match: MatchDto) =>
    !term ||
    [match.homeTeam?.name, match.awayTeam?.name, match.matchType, match.format]
      .filter(Boolean)
      .join(" ")
      .toLowerCase()
      .includes(term);

  const visibleScheduled = useMemo(() => scheduled.filter(matchesSearch), [scheduled, term]);
  const visibleCompleted = useMemo(() => completed.filter(matchesSearch), [completed, term]);

  return (
    <>
      <PageHeader
        eyebrow="Розклад"
        title="Матчі"
        description="Найближчі ігри та зіграні результати всіх турнірів."
        action={
          canEdit && (
            <Link to="/matches/new" className="btn btn-primary">
              <PlusCircle className="h-4 w-4" />
              Додати матч
            </Link>
          )
        }
      />

      <SearchField value={search} onChange={setSearch} placeholder="Команда, формат або стадія" />

      {loading && <Skeleton rows={4} />}

      {!loading && (
        <div className="space-y-5">
          <section className="panel">
            <div className="panel-header">
              <h2 className="section-title">Заплановані</h2>
              <span className="tabular font-mono text-micro text-text-faint">{visibleScheduled.length}</span>
            </div>
            <div className="panel-body">
              {visibleScheduled.length === 0 ? (
                <EmptyState
                  title={term ? "Нічого не знайдено" : "Немає запланованих матчів"}
                  hint={
                    term
                      ? "Спробуйте іншу назву команди або формат."
                      : "Матчі створюються вручну або автоматично під час генерації турнірної сітки."
                  }
                />
              ) : (
                <ul className="divide-y divide-line-soft">
                  {visibleScheduled.map((match) => (
                    <MatchRow
                      key={match.id}
                      match={match}
                      showScore={false}
                      canEdit={canEdit}
                      onDelete={handleDelete}
                    />
                  ))}
                </ul>
              )}
            </div>
          </section>

          <section className="panel">
            <div className="panel-header">
              <h2 className="section-title">Зіграні</h2>
              <span className="tabular font-mono text-micro text-text-faint">{visibleCompleted.length}</span>
            </div>
            <div className="panel-body">
              {visibleCompleted.length === 0 ? (
                <EmptyState title="Результатів ще немає" hint="Тут зʼявляться завершені матчі з рахунком." />
              ) : (
                <ul className="divide-y divide-line-soft">
                  {visibleCompleted.map((match) => (
                    <MatchRow
                      key={match.id}
                      match={match}
                      showScore
                      canEdit={canEdit}
                      onDelete={handleDelete}
                    />
                  ))}
                </ul>
              )}
            </div>
          </section>
        </div>
      )}
    </>
  );
};

export default MatchesSchedule;
