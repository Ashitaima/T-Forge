import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { DraftDto } from "../types";

export const draftsApi = {
  /** 404 означає «драфту немає» — це нормальний стан, а не помилка. */
  getForMatch: async (matchId: number) => {
    const response = await apiClient.get<DraftDto>(endpoints.matchDraft(matchId));
    return response.data;
  },
  create: async (matchId: number, turnSeconds?: number) => {
    const response = await apiClient.post<DraftDto>(endpoints.matchDraft(matchId), { turnSeconds });
    return response.data;
  },
  start: async (draftId: number) => {
    const response = await apiClient.post<DraftDto>(`${endpoints.drafts}/${draftId}/start`);
    return response.data;
  },
  /**
   * stepIndex їде в шляху навмисно: сервер застосує вибір лише якщо драфт досі
   * чекає саме на цей крок. Інакше натискання за мить до спрацювання таймера
   * перетерло б те, що таймер уже записав.
   */
  applyStep: async (draftId: number, stepIndex: number, value: string) => {
    const response = await apiClient.post<DraftDto>(
      `${endpoints.drafts}/${draftId}/steps/${stepIndex}`,
      { value }
    );
    return response.data;
  },
  cancel: async (draftId: number) => {
    await apiClient.delete(`${endpoints.drafts}/${draftId}`);
  }
};
