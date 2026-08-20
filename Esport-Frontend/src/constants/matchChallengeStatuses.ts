/**
 * Дзеркало Esport-Backend/Common/MatchChallengeStatus.cs.
 *
 * Виклик живе окремо від матчу, який із нього народжується: у виклику є
 * Pending і Declined, а рахунку й перебігу немає. Той самий поділ, що у
 * duelStatuses.ts — тут підписи, там правило.
 */
export const MATCH_CHALLENGE_STATUS = {
  Pending: "Pending",
  Accepted: "Accepted",
  Declined: "Declined",
  Cancelled: "Cancelled"
} as const;

export const MATCH_CHALLENGE_STATUS_LABELS: Record<string, string> = {
  Pending: "Чекає відповіді",
  Accepted: "Прийнято",
  Declined: "Відхилено",
  Cancelled: "Скасовано"
};

/** Класи пігулок беремо ті самі, що й у дуелей. */
export const MATCH_CHALLENGE_STATUS_PILL: Record<string, string> = {
  Pending: "pill-neutral",
  Accepted: "pill-done",
  Declined: "pill-off",
  Cancelled: "pill-off"
};

export const matchChallengeStatusLabel = (status: string): string =>
  MATCH_CHALLENGE_STATUS_LABELS[status] ?? status;
