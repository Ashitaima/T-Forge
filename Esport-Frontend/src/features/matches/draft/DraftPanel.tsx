import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { draftsApi } from "../../../api/draftsApi";
import { subscribeToMatch } from "../../../api/matchHub";
import {
  ACTION_LABELS,
  DRAFT_ACTION,
  DRAFT_STATUS,
  DRAFT_STATUS_LABELS,
  choiceLabel,
  consumesFromPool
} from "../../../constants/draft";
import { MAP_NAMES } from "../../../constants/gameMaps";
import { SIDE_NAMES } from "../../../constants/gameSides";
import { HERO_NAMES } from "../../../constants/gameHeroes";
import { useSubmitError } from "../../../hooks/useSubmitError";
import { DraftBoard } from "./DraftBoard";
import { DraftTimeline } from "./DraftTimeline";
import type { DraftDto } from "../../../types";

type TeamRef = { id: number; name: string };

type Props = {
  matchId: number;
  homeTeam: TeamRef;
  awayTeam: TeamRef | null;
  /** Через useIsCaptainOf у батька — інакше режим розробника працює наполовину. */
  isHomeCaptain: boolean;
  isAwayCaptain: boolean;
  canManage: boolean;
};

/** Повний каталог дисципліни для кроку такої дії — дзеркало DraftEngine.CatalogFor. */
const catalogFor = (game: string, action: string) => {
  if (action === DRAFT_ACTION.side) {
    return Object.keys(SIDE_NAMES[game] ?? {});
  }
  return Object.keys(MAP_NAMES[game] ?? HERO_NAMES[game] ?? {});
};

/**
 * Екран драфту.
 *
 * Стан приходить із REST, а хаб лише каже «перечитай»: тримати на клієнті
 * другу копію правил зшивання стану дорожче, ніж один зайвий запит, і саме
 * там така копія розходилася б із сервером.
 */
export const DraftPanel = ({
  matchId,
  homeTeam,
  awayTeam,
  isHomeCaptain,
  isAwayCaptain,
  canManage
}: Props) => {
  const [draft, setDraft] = useState<DraftDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [busyValue, setBusyValue] = useState<string | null>(null);
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null);
  const { error, clear, capture } = useSubmitError();

  // Різниця між годинником сервера й локальним. Без неї користувач, у якого
  // годинник збігає, бачить відлік, що суперечить серверу.
  const clockOffset = useRef(0);

  const applyDraft = useCallback((next: DraftDto) => {
    clockOffset.current = Date.now() - new Date(next.serverTime).getTime();
    setDraft(next);
  }, []);

  const reload = useCallback(async () => {
    try {
      applyDraft(await draftsApi.getForMatch(matchId));
    } catch {
      // 404 — драфту немає, і це нормальний стан, а не помилка.
      setDraft(null);
    } finally {
      setLoading(false);
    }
  }, [matchId, applyDraft]);

  useEffect(() => {
    reload();
  }, [reload]);

  useEffect(
    () => subscribeToMatch(matchId, { onDraftStep: reload, onDraftStatus: reload }),
    [matchId, reload]
  );

  useEffect(() => {
    if (!draft?.turnDeadline || draft.status !== DRAFT_STATUS.inProgress) {
      setSecondsLeft(null);
      return;
    }

    const deadline = new Date(draft.turnDeadline).getTime();
    const tick = () =>
      setSecondsLeft(Math.max(0, Math.ceil((deadline - (Date.now() - clockOffset.current)) / 1000)));

    tick();
    const timer = window.setInterval(tick, 250);
    return () => window.clearInterval(timer);
  }, [draft?.turnDeadline, draft?.status]);

  /** Одне місце, де ловимо відмову сервера: useSubmitError дає capture, не run. */
  const perform = async (action: () => Promise<void>, fallback: string) => {
    clear();
    setBusy(true);
    try {
      await action();
    } catch (caught) {
      capture(caught, fallback);
    } finally {
      setBusy(false);
    }
  };

  const currentStep = draft?.steps.find((step) => step.stepIndex === draft.currentStepIndex) ?? null;

  const takenValues = useMemo(() => {
    if (!draft || !currentStep || !consumesFromPool(currentStep.action)) {
      return new Set<string>();
    }
    return new Set(
      draft.steps
        .filter((step) => step.chosenValue !== null && consumesFromPool(step.action))
        .map((step) => step.chosenValue!)
    );
  }, [draft, currentStep]);

  const teamName = useCallback(
    (teamId: number | null) => {
      if (teamId === homeTeam.id) return homeTeam.name;
      if (awayTeam && teamId === awayTeam.id) return awayTeam.name;
      return "—";
    },
    [homeTeam, awayTeam]
  );

  // Дзеркалить серверний DraftPolicy.CanAct: ходить лише капітан тієї команди,
  // чий зараз хід. Адміністратора тут немає навмисно — він не банить.
  const canAct =
    draft?.status === DRAFT_STATUS.inProgress &&
    currentStep !== null &&
    ((currentStep.actingTeamId === homeTeam.id && isHomeCaptain) ||
      (awayTeam !== null && currentStep.actingTeamId === awayTeam.id && isAwayCaptain));

  const choose = (value: string) => {
    if (!draft || !currentStep) return;
    setBusyValue(value);
    perform(
      async () => applyDraft(await draftsApi.applyStep(draft.id, currentStep.stepIndex, value)),
      "Не вдалося зробити хід."
    ).finally(() => setBusyValue(null));
  };

  if (loading) {
    return <p className="muted text-micro">Завантаження драфту…</p>;
  }

  return (
    <section className="panel">
      <div className="panel-header">
        <h2 className="section-title">Драфт</h2>
        {draft && (
          <span className="pill pill-neutral">
            {DRAFT_STATUS_LABELS[draft.status] ?? draft.status}
          </span>
        )}
      </div>

      <div className="panel-body space-y-4">
        {error && <div className="notice notice-error">{error}</div>}

        {!draft && (
          <>
            <p className="muted text-micro">
              Драфту ще немає. Капітани по черзі забороняють і обирають — сервер веде відлік
              кожного ходу.
            </p>
            {canManage && awayTeam && (
              <button
                type="button"
                className="btn btn-primary btn-sm"
                disabled={busy}
                onClick={() =>
                  perform(
                    async () => applyDraft(await draftsApi.create(matchId)),
                    "Не вдалося створити драфт."
                  )
                }
              >
                Створити драфт
              </button>
            )}
            {canManage && !awayTeam && (
              <p className="muted text-micro">Драфт можливий, коли в матчі буде суперник.</p>
            )}
          </>
        )}

        {draft && (
          <>
            <div className="flex flex-wrap items-center gap-3">
              <span className="text-micro text-text-muted">
                Першим ходить{" "}
                <strong className="text-text">{teamName(draft.firstTurnTeamId)}</strong>
              </span>
              {draft.status === DRAFT_STATUS.inProgress && currentStep && (
                <>
                  <span className="pill pill-live">
                    {ACTION_LABELS[currentStep.action] ?? currentStep.action}:{" "}
                    {teamName(currentStep.actingTeamId)}
                  </span>
                  {secondsLeft !== null && (
                    <span className="tabular font-mono text-ember">{secondsLeft} с</span>
                  )}
                </>
              )}
            </div>

            {draft.status === DRAFT_STATUS.pending && canManage && (
              <button
                type="button"
                className="btn btn-primary btn-sm"
                disabled={busy}
                onClick={() =>
                  perform(
                    async () => applyDraft(await draftsApi.start(draft.id)),
                    "Не вдалося розпочати драфт."
                  )
                }
              >
                Розпочати драфт
              </button>
            )}

            {draft.status === DRAFT_STATUS.inProgress && currentStep && (
              <>
                {!canAct && (
                  <p className="muted text-micro">
                    Зараз хід команди {teamName(currentStep.actingTeamId)}.
                  </p>
                )}
                <DraftBoard
                  game={draft.game}
                  action={currentStep.action}
                  values={catalogFor(draft.game, currentStep.action)}
                  takenValues={takenValues}
                  canAct={Boolean(canAct)}
                  busyValue={busyValue}
                  onChoose={choose}
                />
              </>
            )}

            <DraftTimeline draft={draft} teamName={teamName} />

            {draft.status !== DRAFT_STATUS.completed && canManage && (
              <button
                type="button"
                className="btn btn-danger btn-sm"
                disabled={busy}
                onClick={() =>
                  perform(async () => {
                    await draftsApi.cancel(draft.id);
                    setDraft(null);
                  }, "Не вдалося скасувати драфт.")
                }
              >
                Скасувати драфт
              </button>
            )}

            {draft.status === DRAFT_STATUS.completed && (
              <p className="muted text-micro">
                Драфт завершено:{" "}
                {draft.steps
                  .filter(
                    (step) =>
                      step.action === DRAFT_ACTION.pick || step.action === DRAFT_ACTION.decider
                  )
                  .map((step) => choiceLabel(draft.game, step.action, step.chosenValue ?? ""))
                  .join(", ")}
              </p>
            )}
          </>
        )}
      </div>
    </section>
  );
};

export default DraftPanel;
