/**
 * Дзеркало Esport-Backend/Common/PlayerPositions.cs.
 *
 * Сервер відхиляє позицію, якої немає в цьому списку, тож форми показують
 * саме його — вільне текстове поле неминуче призводило б до помилки 400.
 * Змінюючи список тут, змініть його і на бекенді.
 */
export const PLAYER_POSITIONS = [
  "Support",
  "ADC",
  "Mid",
  "Jungle",
  "Top",
  "IGL",
  "Entry",
  "Lurker",
  "AWPer",
  "Rifler"
] as const;

export type PlayerPosition = (typeof PLAYER_POSITIONS)[number];

/** Межі, які перевіряє PlayerRules на бекенді. */
export const PLAYER_MIN_AGE = 13;
export const PLAYER_MAX_AGE = 50;
export const NICKNAME_MIN_LENGTH = 3;
export const NICKNAME_MAX_LENGTH = 30;
export const NICKNAME_PATTERN = /^[a-zA-Z0-9_]+$/;
