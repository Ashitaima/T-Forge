/**
 * Дзеркало Esport-Backend/Common/PlayerSortKeys.cs та TeamSortKeys.cs.
 *
 * Ключ їде на сервер як ?sortBy=... Невідомий ключ сервер не відхиляє —
 * він просто застосовує типовий порядок, тож розходження проявилося б
 * як «сортування не працює», а не як помилка. Тримайте списки синхронними.
 */
export const PLAYER_SORT_KEYS = [
  "nickname",
  "position",
  "country",
  "team",
  "matches",
  "wins",
  "winRate",
  "kda",
  "rating"
] as const;

export const TEAM_SORT_KEYS = [
  "name",
  "tag",
  "region",
  "played",
  "wins",
  "winRate",
  "titles",
  "rating"
] as const;

export type PlayerSortKey = (typeof PLAYER_SORT_KEYS)[number];
export type TeamSortKey = (typeof TEAM_SORT_KEYS)[number];
export type SortDirection = "asc" | "desc";
