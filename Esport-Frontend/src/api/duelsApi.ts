import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { CompleteDuelDto, CreateDuelDto, DuelDto, DuelRecordDto } from "../types";

export const duelsApi = {
  /** playerId звужує список до дуелей одного гравця. */
  list: async (playerId?: number) => {
    const response = await apiClient.get<DuelDto[]>(endpoints.duels, {
      params: playerId ? { playerId } : undefined
    });
    return response.data;
  },
  getById: async (id: number) => {
    const response = await apiClient.get<DuelDto>(`${endpoints.duels}/${id}`);
    return response.data;
  },
  getRecord: async (playerId: number) => {
    const response = await apiClient.get<DuelRecordDto>(endpoints.duelRecord(playerId));
    return response.data;
  },
  create: async (payload: CreateDuelDto) => {
    const response = await apiClient.post<DuelDto>(endpoints.duels, payload);
    return response.data;
  },
  respond: async (id: number, accept: boolean) => {
    const response = await apiClient.post<DuelDto>(endpoints.duelRespond(id), null, {
      params: { accept }
    });
    return response.data;
  },
  cancel: async (id: number) => {
    const response = await apiClient.post<DuelDto>(endpoints.duelCancel(id));
    return response.data;
  },
  start: async (id: number) => {
    const response = await apiClient.post<DuelDto>(endpoints.duelStart(id));
    return response.data;
  },
  complete: async (id: number, payload: CompleteDuelDto) => {
    const response = await apiClient.post<DuelDto>(endpoints.duelComplete(id), payload);
    return response.data;
  }
};
