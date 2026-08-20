import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { BarChart3, PlusCircle, Radio, Trash2 } from "lucide-react";
import { matchesApi } from "../../api/matchesApi";
import { subscribeToMatch } from "../../api/matchHub";
import { useIsRole } from "../../hooks/useEffectiveRole";
import { useCaptainedTeams } from "../../hooks/useCaptainedTeams";
import { DuelsPanel } from "../duels/DuelsPanel";
import { EmptyState, PageHeader, SearchField, Skeleton, StatusPill } from "../../components/ui/Primitives";
import { Avatar, teamInitials } from "../../components/ui/Avatar";
import { GAMES, GAME_LABELS, gameLabel } from "../../constants/games";
import type { MatchDto } from "../../types";

/** Рядок матчу. Експортується, бо той самий рядок показує і сторінка турніру. */
export const MatchRow = ({
  match,
  showScore,
  canEdit,
  onDelete,
  onJoin,
  canJoin = false
}: {
  match: MatchDto;
  showScore: boolean;
  canEdit: boolean;
  onDelete: (id: number) => void;
  /** Приєднатися до відкритого матчу. Немає — кнопки теж немає. */
  onJoin?: (id: number) => void;
  canJoin?: boolean;
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

      {match.name && (
        <span className="w-full truncate text-micro text-text-faint">{match.name}</span>
      )}

      {/* Команди вирівняні по центральній осі рахунку, як у турнірній таблиці */}
      <div className="flex min-w-[15rem] flex-1 items-center gap-3">
        <span className={`flex min-w-0 flex-1 items-center justify-end gap-2 text-body ${homeWon ? "text-text" : "text-text-muted"}`}>
          <span className="truncate">{match.homeTeam?.name ?? "Очікується"}</span>
          <Avatar
            url={match.homeTeam?.logoPath}
            shape="square"
            size="sm"
            fallback={teamInitials(match.homeTeam?.name, match.homeTeam?.tag)}
            alt=""
          />
        </span>
        {showScore || match.status === "InProgress" ? (
          <span className="tabular shrink-0 rounded-md border border-line bg-ink-950 px-2.5 py-1 font-mono text-body text-text">
            {match.homeTeamScore}:{match.awayTeamScore}
          </span>
        ) : (
          <span className="shrink-0 font-mono text-micro text-text-faint">vs</span>
        )}
        <span className={`flex min-w-0 flex-1 items-center gap-2 text-body ${awayWon ? "text-text" : "text-text-muted"}`}>
          <Avatar
            url={match.awayTeam?.logoPath}
            shape="square"
            size="sm"
            fallback={teamInitials(match.awayTeam?.name, match.awayTeam?.tag)}
            alt=""
          />
          <span className="truncate">
            {match.awayTeam?.name ?? (match.isOpen ? "Відкритий слот" : "Очікується")}
          </span>
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

      {match.isOpen && canJoin && onJoin && (
        <button
          type="button"
          onClick={() => onJoin(match.id)}
          className="btn btn-primary btn-sm"
        >
          Приєднатися
        </button>
      )}

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

  const [tab, setTab] = useState<"scheduled" | "completed">("scheduled");
  const canEdit = useIsRole("Organizer", "Admin");

  // Створити практичний матч може й капітан — це вирішує Team.CaptainId,
  // а не роль, тож canEdit (права на редагування рядків) тут не підходить.
  const { teams: myTeams, isCaptain } = useCaptainedTeams();
  const canCreate = canEdit || isCaptain;

  // Друга вісь сторінки: командний матч чи дуель один на один. Обидва — гра
  // поза турніром, але це різні сутності з різними показниками, тож і списки
  // різні (docs/superpowers/specs/2026-08-19-duel-1v1-design.md).
  const [mode, setMode] = useState<"team" | "duel">("team");

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

  // Приєднатися можна лише до чужого відкритого матчу — своя команда в ньому
  // вже грає. Сервер перевіряє те саме; тут ми просто не малюємо кнопку,
  // яка дасть 403.
  const joinMatch = async (id: number) => {
    const joined = await matchesApi.join(id);
    setScheduled((prev) => prev.map((match) => (match.id === id ? joined : match)));
  };

  const canJoinMatch = (match: MatchDto) =>
    isCaptain && myTeams.every((team) => team.id !== match.homeTeam?.id);

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
    const matchesKind = isFriendly(match);
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
    [scheduled, term, game]
  );
  const visibleCompleted = useMemo(
    () => completed.filter(matchesFilters),
    [completed, term, game]
  );

  const tabs = [
    { id: "scheduled" as const, label: "Заплановані", count: visibleScheduled.length },
    { id: "completed" as const, label: "Зіграні", count: visibleCompleted.length }
  ];

  const activeMatches = tab === "scheduled" ? visibleScheduled : visibleCompleted;

  return (
    <>
      <PageHeader
        eyebrow="Розклад"
        title="Практичні матчі"
        description={
          mode === "duel"
            ? "Дуелі один на один. Окремий рахунок: у статистику командних матчів вони не входять і рейтингу не дають."
            : "Командні матчі поза турнірами. Рахунок і KDA враховуються, але титулів і рейтингу вони не дають. Турнірні матчі — на сторінці свого турніру."
        }
        action={
          mode === "team" &&
          canCreate && (
            <Link to="/matches/new" className="btn btn-primary">
              <PlusCircle className="h-4 w-4" />
              Додати матч
            </Link>
          )
        }
      />

      {/* Командний матч і дуель — різні сутності, тож перемикач стоїть над
          усім іншим: фільтри нижче стосуються лише командного списку. */}
      <div className="flex gap-1 border-b border-line-soft">
        {[
          { id: "team" as const, label: "Командні" },
          { id: "duel" as const, label: "Один на один" }
        ].map(({ id, label }) => (
          <button
            key={id}
            type="button"
            onClick={() => setMode(id)}
            className={`relative px-4 py-2.5 text-lead transition ${
              mode === id ? "text-text" : "text-text-muted hover:text-text"
            }`}
          >
            {label}
            {mode === id && (
              <span className="absolute inset-x-0 -bottom-px h-0.5 rounded-t bg-ember" />
            )}
          </button>
        ))}
      </div>

      {mode === "duel" && <DuelsPanel />}

      {mode === "team" && (
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

      )}

      {mode === "team" && loading && <Skeleton rows={4} />}

      {mode === "team" && !loading && (
        <>
          {/* Заплановані та зіграні — два погляди на той самий розклад.
              Турнірних матчів тут більше немає: вони належать турніру й
              живуть на його сторінці, поруч із сіткою. */}
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
                        : "Капітан може створити матч кнопкою вгорі або прийнявши виклик іншої команди."
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
                      canJoin={canJoinMatch(match)}
                      onJoin={joinMatch}
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
