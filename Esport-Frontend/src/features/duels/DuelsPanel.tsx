import { useCallback, useEffect, useState } from "react";
import { Swords } from "lucide-react";
import { duelsApi } from "../../api/duelsApi";
import { playersApi } from "../../api/playersApi";
import { useAuthStore } from "../../store/authStore";
import { useSubmitError } from "../../hooks/useSubmitError";
import { EmptyState, Skeleton } from "../../components/ui/Primitives";
import { DateTimePicker } from "../../components/ui/DateTimePicker";
import { GAMES, GAME_LABELS, gameLabel } from "../../constants/games";
import {
  DUEL_STATUS,
  DUEL_STATUS_PILL,
  duelStatusLabel,
  isDuelPlayable
} from "../../constants/duelStatuses";
import type { DuelDto, PlayerRowDto } from "../../types";

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("uk-UA", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit"
  });

/**
 * Рядок дуелі.
 *
 * Кнопки повторюють Common/DuelPolicy.cs: відповідає лише викликаний,
 * скасовує лише ініціатор і лише до відповіді, ведуть обидва. Сервер усе
 * одно перевіряє те саме — тут ми просто не показуємо того, що дасть 403.
 */
const DuelRow = ({
  duel,
  userId,
  onAction
}: {
  duel: DuelDto;
  userId: number | null;
  onAction: (run: () => Promise<unknown>) => void;
}) => {
  const [scoring, setScoring] = useState(false);
  const [challengerScore, setChallengerScore] = useState(duel.challengerScore);
  const [opponentScore, setOpponentScore] = useState(duel.opponentScore);

  const isChallenger = userId !== null && userId === duel.challengerUserId;
  const isOpponent = userId !== null && userId === duel.opponentUserId;
  const isParticipant = isChallenger || isOpponent;

  // Відкритий виклик приймає будь-хто, крім автора — так само, як вирішує
  // Common/DuelPolicy.cs. Адресний — лише той, кого назвали.
  const openToMe =
    duel.isOpen && duel.status === DUEL_STATUS.Pending && userId !== null && !isChallenger;
  const awaitingMe = duel.status === DUEL_STATUS.Pending && isOpponent;
  const canCancel = duel.status === DUEL_STATUS.Pending && isChallenger;
  const canManage = isParticipant && isDuelPlayable(duel.status);

  const done = duel.status === DUEL_STATUS.Completed;
  const challengerWon = done && duel.winnerPlayerId === duel.challengerPlayer?.id;
  const opponentWon = done && duel.winnerPlayerId === duel.opponentPlayer?.id;

  const side = (won: boolean) =>
    `truncate text-body ${won ? "font-semibold text-text" : "text-text-muted"}`;

  const complete = () => {
    // Нічия — це нормальний результат, а не пропущене поле: рівний рахунок
    // віддаємо з winnerPlayerId = null, і DuelRecordCalculator рахує її окремо.
    const winnerPlayerId =
      challengerScore === opponentScore
        ? null
        : challengerScore > opponentScore
          ? (duel.challengerPlayer?.id ?? null)
          : (duel.opponentPlayer?.id ?? null);

    onAction(() => duelsApi.complete(duel.id, { challengerScore, opponentScore, winnerPlayerId }));
    setScoring(false);
  };

  return (
    <div className="surface-raised px-4 py-3.5">
      <div className="flex flex-wrap items-center gap-3">
        <div className="tabular w-24 shrink-0 font-mono text-micro text-text-faint">
          {formatDateTime(duel.scheduledAt)}
        </div>

        <div className="flex min-w-0 flex-1 items-center gap-2">
          <span className={side(challengerWon)}>{duel.challengerPlayer?.nickname ?? "—"}</span>
          {done ? (
            <span className="tabular shrink-0 rounded-md border border-line bg-ink-950 px-2.5 py-1 font-mono text-body text-text">
              {duel.challengerScore}:{duel.opponentScore}
            </span>
          ) : (
            <Swords className="h-3.5 w-3.5 shrink-0 text-text-faint" />
          )}
          {duel.opponentPlayer ? (
            <span className={side(opponentWon)}>{duel.opponentPlayer.nickname}</span>
          ) : (
            <span className="truncate text-body text-text-faint">Суперника ще немає</span>
          )}
        </div>

        <span className="shrink-0 text-micro text-text-faint">{gameLabel(duel.game)}</span>
        {duel.isOpen && duel.status === DUEL_STATUS.Pending && (
          <span className="pill shrink-0 pill-neutral">Відкритий</span>
        )}
        <span className={`pill shrink-0 ${DUEL_STATUS_PILL[duel.status] ?? "pill-neutral"}`}>
          {duelStatusLabel(duel.status)}
        </span>

        <div className="row-actions ml-auto">
          {openToMe && (
            <button
              type="button"
              onClick={() => onAction(() => duelsApi.respond(duel.id, true))}
              className="btn btn-primary btn-sm"
            >
              Прийняти виклик
            </button>
          )}

          {awaitingMe && (
            <>
              <button
                type="button"
                onClick={() => onAction(() => duelsApi.respond(duel.id, true))}
                className="btn btn-primary btn-sm"
              >
                Прийняти
              </button>
              <button
                type="button"
                onClick={() => onAction(() => duelsApi.respond(duel.id, false))}
                className="btn btn-ghost btn-sm"
              >
                Відхилити
              </button>
            </>
          )}

          {canCancel && (
            <button
              type="button"
              onClick={() => onAction(() => duelsApi.cancel(duel.id))}
              className="btn btn-ghost btn-sm"
            >
              Скасувати
            </button>
          )}

          {canManage && duel.status === DUEL_STATUS.Accepted && (
            <button
              type="button"
              onClick={() => onAction(() => duelsApi.start(duel.id))}
              className="btn btn-secondary btn-sm"
            >
              Почати
            </button>
          )}

          {canManage && duel.status === DUEL_STATUS.InProgress && !scoring && (
            <button type="button" onClick={() => setScoring(true)} className="btn btn-primary btn-sm">
              Завершити
            </button>
          )}
        </div>
      </div>

      {duel.message && !done && (
        <p className="muted mt-2 pl-[6.75rem] text-micro">{duel.message}</p>
      )}

      {scoring && (
        <div className="mt-3 flex flex-wrap items-end gap-3 border-t border-line-soft pt-3">
          <label className="field">
            {duel.challengerPlayer?.nickname}
            <input
              type="number"
              min={0}
              value={challengerScore}
              onChange={(event) => setChallengerScore(Number(event.target.value))}
              className="input tabular w-24 font-mono"
            />
          </label>
          <label className="field">
            {duel.opponentPlayer?.nickname}
            <input
              type="number"
              min={0}
              value={opponentScore}
              onChange={(event) => setOpponentScore(Number(event.target.value))}
              className="input tabular w-24 font-mono"
            />
          </label>
          <button type="button" onClick={complete} className="btn btn-primary">
            Зберегти результат
          </button>
          <button type="button" onClick={() => setScoring(false)} className="btn btn-ghost">
            Скасувати
          </button>
          {challengerScore === opponentScore && (
            <p className="field-hint w-full">Рівний рахунок буде записано як нічию.</p>
          )}
        </div>
      )}
    </div>
  );
};

/**
 * Дуелі 1 на 1.
 *
 * Живуть поруч із практичними матчами, бо це теж гра поза турніром — але
 * власною сутністю: показники дуелей навмисно не входять у статистику матчів
 * (docs/superpowers/specs/2026-08-19-duel-1v1-design.md).
 */
export const DuelsPanel = () => {
  const user = useAuthStore((state) => state.user);
  const [duels, setDuels] = useState<DuelDto[]>([]);
  const [players, setPlayers] = useState<PlayerRowDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const submitError = useSubmitError();

  const [isOpenChallenge, setIsOpenChallenge] = useState(false);
  const [opponentPlayerId, setOpponentPlayerId] = useState("");
  const [game, setGame] = useState("");
  const [scheduledAt, setScheduledAt] = useState("");
  const [message, setMessage] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setDuels(await duelsApi.list());
    } catch {
      setDuels([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  // Список суперників потрібен лише тому, хто справді може викликати.
  useEffect(() => {
    if (!user) {
      setPlayers([]);
      return;
    }

    let isActive = true;

    playersApi
      .getPaged({ page: 1, pageSize: 100, sortBy: "nickname", sortDirection: "asc" })
      .then((response) => isActive && setPlayers(response.data))
      .catch(() => isActive && setPlayers([]));

    return () => {
      isActive = false;
    };
  }, [user]);

  const run = async (action: () => Promise<unknown>) => {
    submitError.clear();
    try {
      await action();
      await load();
    } catch (error) {
      submitError.capture(error);
    }
  };

  const submit = async () => {
    if ((!isOpenChallenge && !opponentPlayerId) || !game || !scheduledAt) {
      return;
    }

    await run(() =>
      duelsApi.create({
        // Відкритий виклик іде без суперника — його назве той, хто прийме.
        opponentPlayerId: isOpenChallenge ? null : Number(opponentPlayerId),
        game,
        scheduledAt,
        message
      })
    );

    setCreating(false);
    setOpponentPlayerId("");
    setMessage("");
  };

  // Себе в списку суперників бути не повинно — сервер це відхилить, але
  // пропонувати таке однаково не варто.
  const opponents = players.filter((player) => player.userId !== user?.id);

  return (
    <div className="space-y-4">
      {user && (
        <div className="flex justify-end">
          <button
            type="button"
            onClick={() => setCreating((current) => !current)}
            className="btn btn-primary"
          >
            <Swords className="h-4 w-4" />
            {creating ? "Згорнути" : "Викликати на дуель"}
          </button>
        </div>
      )}

      {creating && (
        <section className="panel panel-body space-y-4">
          {/* Відкритий виклик — оголошення на дошці: суперника не названо,
              і прийняти його може будь-хто, крім автора. Закритий — адресний,
              як було. */}
          <div className="field">
            Кому
            <div className="mt-1.5 flex gap-1.5">
              {[
                { open: false, label: "Конкретному гравцю" },
                { open: true, label: "Відкритий виклик" }
              ].map(({ open, label }) => (
                <button
                  key={label}
                  type="button"
                  onClick={() => setIsOpenChallenge(open)}
                  aria-pressed={isOpenChallenge === open}
                  className={`rounded-lg px-3 py-1.5 text-micro transition ${
                    isOpenChallenge === open
                      ? "bg-ink-800 text-text"
                      : "text-text-muted hover:bg-ink-900 hover:text-text"
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          {isOpenChallenge ? (
            <p className="rounded-lg border border-line bg-ink-800/60 px-3 py-2.5 text-micro text-text-muted">
              Виклик побачать усі гравці. Дуель почнеться з тим, хто прийме його першим.
            </p>
          ) : (
            <label className="field">
              Суперник
              <select
                value={opponentPlayerId}
                onChange={(event) => setOpponentPlayerId(event.target.value)}
                className="input"
              >
                <option value="">Оберіть гравця</option>
                {opponents.map((player) => (
                  <option key={player.id} value={player.id}>
                    {player.nickname}
                  </option>
                ))}
              </select>
            </label>
          )}

          <label className="field">
            Дисципліна
            <select value={game} onChange={(event) => setGame(event.target.value)} className="input">
              <option value="">Оберіть дисципліну</option>
              {GAMES.map((value) => (
                <option key={value} value={value}>
                  {GAME_LABELS[value]}
                </option>
              ))}
            </select>
          </label>

          <div className="field">
            Час
            <div className="mt-1.5">
              <DateTimePicker
                ariaLabel="Час дуелі"
                value={scheduledAt}
                onChange={setScheduledAt}
              />
            </div>
          </div>

          <label className="field">
            Повідомлення
            <input
              type="text"
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              placeholder="Необовʼязково"
              className="input"
            />
          </label>

          {submitError.error && <div className="notice notice-error">{submitError.error}</div>}

          <div className="flex items-center gap-3 border-t border-line-soft pt-4">
            <button
              type="button"
              onClick={submit}
              disabled={(!isOpenChallenge && !opponentPlayerId) || !game || !scheduledAt}
              className="btn btn-primary"
            >
              {isOpenChallenge ? "Опублікувати виклик" : "Надіслати виклик"}
            </button>
            <button type="button" onClick={() => setCreating(false)} className="btn btn-ghost">
              Скасувати
            </button>
          </div>
        </section>
      )}

      {!creating && submitError.error && (
        <div className="notice notice-error">{submitError.error}</div>
      )}

      <section className="panel">
        <div className="panel-body space-y-3">
          {loading && <Skeleton rows={3} />}

          {!loading && duels.length === 0 && (
            <EmptyState
              title="Дуелей ще немає"
              hint="Виклик на дуель — це матч один на один, без команди. Титулів і рейтингу він не дає."
            />
          )}

          {!loading &&
            duels.map((duel) => (
              <DuelRow key={duel.id} duel={duel} userId={user?.id ?? null} onAction={run} />
            ))}
        </div>
      </section>
    </div>
  );
};

export default DuelsPanel;
