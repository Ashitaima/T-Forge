import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { DuelPredictionDto, MatchPredictionDto } from "../types";

/** Прогноз шахових партій. Публічне читання; available = false — «прогнозу немає», не помилка. */
export const predictionsApi = {
  duel: async (id: number) =>
    (await apiClient.get<DuelPredictionDto>(endpoints.duelPrediction(id))).data,
  match: async (id: number) =>
    (await apiClient.get<MatchPredictionDto>(endpoints.matchPrediction(id))).data
};
