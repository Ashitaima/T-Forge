/**
 * Дзеркало Esport-Backend/Common/TeamOpeningStatus.cs — там значення, тут підписи.
 *
 * Filled ставить шлях прийняття запиту (свайп на кандидата), Cancelled —
 * капітан; Open лишається, доки вакансію не закрили.
 */
export const TEAM_OPENING_STATUS = {
  Open: "Open",
  Filled: "Filled",
  Cancelled: "Cancelled"
} as const;

export const TEAM_OPENING_STATUS_LABELS: Record<string, string> = {
  Open: "Відкрита",
  Filled: "Заповнена",
  Cancelled: "Скасована"
};

/** Класи пігулок беремо ті самі, що й у викликів на матч. */
export const TEAM_OPENING_STATUS_PILL: Record<string, string> = {
  Open: "pill-neutral",
  Filled: "pill-done",
  Cancelled: "pill-off"
};

export const teamOpeningStatusLabel = (status: string): string =>
  TEAM_OPENING_STATUS_LABELS[status] ?? status;

export const isOpeningOpen = (status: string): boolean => status === TEAM_OPENING_STATUS.Open;
