/**
 * Дзеркало Esport-Backend/Common/DuelStatuses.cs.
 *
 * Перелік навмисно не збігається зі станами матчу: у дуелі є Pending і
 * Declined, а Postponed немає. Той самий поділ, що у games.ts — тут підписи,
 * там правило.
 */
export const DUEL_STATUS = {
  Pending: "Pending",
  Accepted: "Accepted",
  Declined: "Declined",
  InProgress: "InProgress",
  Completed: "Completed",
  Cancelled: "Cancelled"
} as const;

export const DUEL_STATUS_LABELS: Record<string, string> = {
  Pending: "Чекає відповіді",
  Accepted: "Заплановано",
  Declined: "Відхилено",
  InProgress: "Триває",
  Completed: "Завершено",
  Cancelled: "Скасовано"
};

/** Класи пігулок беремо ті самі, що й у StatusPill для матчів. */
export const DUEL_STATUS_PILL: Record<string, string> = {
  Pending: "pill-neutral",
  Accepted: "pill-neutral",
  Declined: "pill-off",
  InProgress: "pill-live",
  Completed: "pill-done",
  Cancelled: "pill-off"
};

export const duelStatusLabel = (status: string) => DUEL_STATUS_LABELS[status] ?? status;

/** Грати можна лише після згоди й доки дуель не закрито. */
export const isDuelPlayable = (status: string) =>
  status === DUEL_STATUS.Accepted || status === DUEL_STATUS.InProgress;

export const isDuelFinal = (status: string) =>
  status === DUEL_STATUS.Declined ||
  status === DUEL_STATUS.Completed ||
  status === DUEL_STATUS.Cancelled;
