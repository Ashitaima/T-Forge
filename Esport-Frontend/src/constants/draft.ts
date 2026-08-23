import { gameHasMaps, mapLabel } from "./gameMaps";
import { sideLabel } from "./gameSides";
import { heroName, heroPortraitUrl } from "./gameHeroes";

/** Дзеркало Common/DraftStatuses.cs. */
export const DRAFT_STATUS = {
  pending: "Pending",
  inProgress: "InProgress",
  completed: "Completed",
  cancelled: "Cancelled"
} as const;

/** Дзеркало Common/DraftActions.cs. */
export const DRAFT_ACTION = {
  ban: "Ban",
  pick: "Pick",
  side: "Side",
  decider: "Decider"
} as const;

export const ACTION_LABELS: Record<string, string> = {
  Ban: "Бан",
  Pick: "Пік",
  Side: "Сторона",
  Decider: "Вирішальна"
};

export const DRAFT_STATUS_LABELS: Record<string, string> = {
  Pending: "Не розпочато",
  InProgress: "Триває",
  Completed: "Завершено",
  Cancelled: "Скасовано"
};

/**
 * Чи вилучає вибір цього кроку значення з пулу — дзеркало
 * DraftActions.ConsumesFromPool.
 *
 * Сторону обирають на кожній карті наново, тож «CT» може повторитися;
 * забанену карту — ні.
 */
export const consumesFromPool = (action: string) => action !== DRAFT_ACTION.side;

/**
 * Підпис вибору. Крок сторони бере назви сторін, решта — карти або героїв,
 * залежно від дисципліни.
 */
export const choiceLabel = (game: string, action: string, value: string) => {
  if (action === DRAFT_ACTION.side) {
    return sideLabel(game, value);
  }
  return gameHasMaps(game) ? mapLabel(game, value) : heroName(game, value);
};

/** Портрет героя; для карт і сторін зображення немає. */
export const choicePortraitUrl = (game: string, action: string, value: string) =>
  action !== DRAFT_ACTION.side && !gameHasMaps(game) ? heroPortraitUrl(game, value) : null;
