import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { BarChart3, PlusCircle, Radio, Trash2 } from "lucide-react";
import { matchesApi } from "../../api/matchesApi";
import { subscribeToMatch } from "../../api/matchHub";
import { useIsRole } from "../../hooks/useEffectiveRole";
import { EmptyState, PageHeader, SearchField, Skeleton, StatusPill } from "../../components/ui/Primitives";
import { GAMES, GAME_LABELS, gameLabel } from "../../constants/games";
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
        {showScore || match.status === "InProgress" ? (
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
        <span className="pill">{gameLabel(match.game)}</span>
        {/* Усередині вкладки тип матчу вже відомий, тож замість підпису
            «Товариський» корисніше показати, який це турнір. */}
        {match.tournament?.name && (
          <span className="max-w-[10rem] truncate text-micro text-text-faint" title={match.tournament.name}>
            {match.tournament.name}
          </span>
        )}
        <span className="font-mono text-micro text-text-faint">{match.format}</span>
        <StatusPill status={match.status} />
        {/* Трансляція показується лише для матчу, що йде: у запланованого
            дивитися ще нема чого, а у зіграного посилання веде в порожнечу.
            Для зіграного цю роль виконує посилання на трекер нижче. */}
        {match.streamUrl && match.status === "InProgress" && (
          <a
            href={match.streamUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="btn btn-sm btn-primary"
          >
            <Radio className="h-4 w-4" />
            Дивитися
          </a>
        )}
        {match.trackerUrl && (
          <a
            href={match.trackerUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="btn btn-ghost btn-sm"
            title="Статистика матчу в трекері"
          >
            <BarChart3 className="h-4 w-4" />
          </a>
        )}
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
  const [game, setGame] = useState("");
  // Дві незалежні осі: тип матчу — це різні змагання, статус — різні погляди
  // на той самий розклад. Тому тип нагорі вкладками, статус — перемикачем
  // усередині кожної.
  const [kind, setKind] = useState<"tournament" | "friendly">("tournament");
  const [tab, setTab] = useState<"scheduled" | "completed">("scheduled");
  const canEdit = useIsRole("Organizer", "Admin");

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

  // Живі оновлення для матчів, що вже йдуть. Перепідписуємось лише коли
  // змінюється набір таких матчів, а не на кожен тік рахунку — інакше кожне
  // оновлення рвало б і відновлювало підписку.
  const liveKey = [...scheduled, ...completed]
    .filter((match) => match.status === "InProgress")
    .map((match) => match.id)
    .join(",");

  useEffect(() => {
    if (!liveKey) {
      return;
    }

    const patch = (id: number, next: Partial<MatchDto>) => {
      const apply = (rows: MatchDto[]) => rows.map((row) => (row.id === id ? { ...row, ...next } : row));
      setScheduled(apply);
      setCompleted(apply);
    };

    const unsubscribes = liveKey.split(",").map((raw) => {
      const id = Number(raw);
      return subscribeToMatch(id, {
        onScore: (update) =>
          patch(update.matchId, {
            homeTeamScore: update.homeTeamScore,
            awayTeamScore: update.awayTeamScore
          }),
        onStatus: (update) =>
          patch(update.matchId, {
            status: update.status,
            homeTeamScore: update.homeTeamScore,
            awayTeamScore: update.awayTeamScore
          })
      });
    });

    return () => unsubscribes.forEach((unsubscribe) => unsubscribe());
  }, [liveKey]);

  const handleDelete = async (id: number) => {
    if (!window.confirm("Видалити матч? Дію не можна скасувати.")) {
      return;
    }
    await matchesApi.remove(id);
    setScheduled((prev) => prev.filter((match) => match.id !== id));
    setCompleted((prev) => prev.filter((match) => match.id !== id));
  };

  const term = search.trim().toLowerCase();

  /** Товариський матч — це матч без турніру (Match.TournamentId == null). */
  const isFriendly = (match: MatchDto) => match.tournamentId === null;

  const matchesFilters = (match: MatchDto) => {
    const matchesKind = kind === "friendly" ? isFriendly(match) : !isFriendly(match);
    const matchesGame = !game || match.game === game;
    const matchesTerm =
      !term ||
      [
        match.homeTeam?.name,
        match.awayTeam?.name,
        match.matchType,
        match.format,
        match.tournament?.name,
        gameLabel(match.game)
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase()
        .includes(term);

    return matchesKind && matchesGame && matchesTerm;
  };

  const visibleScheduled = useMemo(
    () => scheduled.filter(matchesFilters),
    [scheduled, term, game, kind]
  );
  const visibleCompleted = useMemo(
    () => completed.filter(matchesFilters),
    [completed, term, game, kind]
  );

  // Лічильники в назвах вкладок рахуються без фільтра за типом — інакше
  // неактивна вкладка завжди показувала б нуль.
  const countFor = (target: "tournament" | "friendly") =>
    [...scheduled, ...completed].filter((match) =>
      target === "friendly" ? isFriendly(match) : !isFriendly(match)
    ).length;

  const kinds = [
    { id: "tournament" as const, label: "Турніри", count: countFor("tournament") },
    { id: "friendly" as const, label: "Практичні", count: countFor("friendly") }
  ];

  const tabs = [
    { id: "scheduled" as const, label: "Заплановані", count: visibleScheduled.length },
    { id: "completed" as const, label: "Зіграні", count: visibleCompleted.length }
  ];

  const activeMatches = tab === "scheduled" ? visibleScheduled : visibleCompleted;

  return (
    <>
      <PageHeader
        eyebrow="Розклад"
        title="Матчі"
        description={
          kind === "friendly"
            ? "Практичні матчі між командами. Рахунок і KDA враховуються, але титулів і рейтингу вони не дають."
            : "Найближчі ігри та зіграні результати всіх турнірів."
        }
        action={
          canEdit && (
            <Link to="/matches/new" className="btn btn-primary">
              <PlusCircle className="h-4 w-4" />
              Додати матч
            </Link>
          )
        }
      />

      <div className="flex flex-wrap items-center gap-3">
        <div className="min-w-[16rem] flex-1">
          <SearchField value={search} onChange={setSearch} placeholder="Команда, формат або стадія" />
        </div>
        <select
          value={game}
          onChange={(event) => setGame(event.target.value)}
          className="input w-auto"
          aria-label="Фільтр за дисципліною"
        >
          <option value="">Усі дисципліни</option>
          {GAMES.map((value) => (
            <option key={value} value={value}>
              {GAME_LABELS[value]}
            </option>
          ))}
        </select>
      </div>

      {loading && <Skeleton rows={4} />}

      {!loading && (
        <>
          {/* Турнірні та практичні — це різні змагання з різними правилами
              (практичні не дають титулів і не рухають рейтинг), тож вони
              стоять першою віссю. */}
          <div className="flex gap-1 border-b border-line-soft">
            {kinds.map(({ id, label, count }) => (
              <button
                key={id}
                type="button"
                onClick={() => setKind(id)}
                className={`relative px-4 py-2.5 text-lead transition ${
                  kind === id ? "text-text" : "text-text-muted hover:text-text"
                }`}
              >
                {label}
                <span className="tabular ml-2 font-mono text-micro text-text-faint">{count}</span>
                {kind === id && (
                  <span className="absolute inset-x-0 -bottom-px h-0.5 rounded-t bg-ember" />
                )}
              </button>
            ))}
          </div>

          {/* Заплановані та зіграні — це два погляди на той самий розклад,
              тож вони другою віссю, всередині обраного типу. */}
          <div className="flex gap-1.5">
            {tabs.map(({ id, label, count }) => (
              <button
                key={id}
                type="button"
                onClick={() => setTab(id)}
                aria-pressed={tab === id}
                className={`rounded-lg px-3 py-1.5 text-micro transition ${
                  tab === id
                    ? "bg-ink-800 text-text"
                    : "text-text-muted hover:bg-ink-900 hover:text-text"
                }`}
              >
                {label}
                <span className="tabular ml-2 font-mono text-text-faint">{count}</span>
              </button>
            ))}
          </div>

          <section className="panel">
            <div className="panel-body">
              {activeMatches.length === 0 ? (
                tab === "scheduled" ? (
                  <EmptyState
                    title={term || game ? "Нічого не знайдено" : "Немає запланованих матчів"}
                    hint={
                      term || game
                        ? "Спробуйте іншу назву команди, формат або дисципліну."
                        : kind === "friendly"
                          ? "Практичні матчі зʼявляються, коли капітан приймає виклик іншої команди."
                          : "Матчі створюються вручну або автоматично під час генерації турнірної сітки."
                    }
                  />
                ) : (
                  <EmptyState
                    title={term || game ? "Нічого не знайдено" : "Результатів ще немає"}
                    hint={
                      term || game
                        ? "Спробуйте інший фільтр."
                        : "Тут зʼявляться завершені матчі з рахунком."
                    }
                  />
                )
              ) : (
                <ul className="divide-y divide-line-soft">
                  {activeMatches.map((match) => (
                    <MatchRow
                      key={match.id}
                      match={match}
                      showScore={tab === "completed"}
                      canEdit={canEdit}
                      onDelete={handleDelete}
                    />
                  ))}
                </ul>
              )}
            </div>
          </section>
        </>
      )}

    </>
  );
};

export default MatchesSchedule;
