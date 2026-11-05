import { GAME_RANKS } from "./gameRanks";

/**
 * Дзеркало Esport-Backend/Common/ChessRatings.cs.
 *
 * Смуга виводиться з точного рейтингу — сервер робить те саме, тож форма
 * лише показує, що збережеться, і не дає вибрати смугу, яка рейтингу суперечить.
 */
export const CHESS_RATING_MIN = 100;
export const CHESS_RATING_MAX = 3500;

export const chessBandFor = (rating: number): string => {
  const bands = GAME_RANKS.Chess;
  for (let i = bands.length - 1; i > 0; i--) {
    if (rating >= Number(bands[i].slice("Chess".length))) {
      return bands[i];
    }
  }
  return bands[0];
};
