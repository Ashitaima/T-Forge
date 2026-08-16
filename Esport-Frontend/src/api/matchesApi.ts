import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type {
  CreateMatchDto,
  CreateMatchPlayerDto,
  MatchDto,
  MatchPlayerDto,
  PagedResponse,
  UpdateMatchDto,
  UpdateMatchPlayerDto,
  UpdateScoreDto
} from "../types";

export const matchesApi = {
  getPaged: async (params: Record<string, string | number | boolean | undefined>) => {
    const response = await apiClient.get<PagedResponse<MatchDto>>(endpoints.matchesPaged, { params });
    return response.data;
  },
  getScheduled: async () => {
    const response = await apiClient.get<MatchDto[]>(endpoints.matchesScheduled);
    return response.data;
  },
  getLive: async () => {
    const response = await apiClient.get<MatchDto[]>(endpoints.matchesLive);
    return response.data;
  },
  getCompleted: async () => {
    const response = await apiClient.get<MatchDto[]>(endpoints.matchesCompleted);
    return response.data;
  },
  getById: async (id: number) => {
    const response = await apiClient.get<MatchDto>(`${endpoints.matches}/${id}`);
    return response.data;
  },
  getDetails: async (id: number) => {
    const response = await apiClient.get<MatchDto>(`${endpoints.matches}/${id}/details`);
    return response.data;
  },
  create: async (payload: CreateMatchDto) => {
    const response = await apiClient.post<MatchDto>(endpoints.matches, payload);
    return response.data;
  },
  update: async (id: number, payload: UpdateMatchDto) => {
    const response = await apiClient.put<MatchDto>(`${endpoints.matches}/${id}`, payload);
    return response.data;
  },
  remove: async (id: number) => {
    await apiClient.delete(`${endpoints.matches}/${id}`);
  },
  start: async (id: number) => {
    await apiClient.post(`${endpoints.matches}/${id}/start`);
  },
  complete: async (
    id: number,
    payload: { winnerTeamId?: number | null; result?: string | null; trackerUrl?: string | null }
  ) => {
    await apiClient.post(`${endpoints.matches}/${id}/complete`, payload);
  },
  cancel: async (id: number, payload: { reason?: string | null }) => {
    await apiClient.post(`${endpoints.matches}/${id}/cancel`, payload);
  },
  updateScore: async (id: number, payload: UpdateScoreDto) => {
    const response = await apiClient.put<MatchDto>(endpoints.matchScore(id), payload);
    return response.data;
  },
  updateLinks: async (id: number, links: { streamUrl: string | null; trackerUrl: string | null }) => {
    const response = await apiClient.put<MatchDto>(endpoints.matchLinks(id), links);
    return response.data;
  },
  getRoster: async (id: number) => {
    const response = await apiClient.get<MatchPlayerDto[]>(endpoints.matchPlayers(id));
    return response.data;
  },
  autofillRoster: async (id: number) => {
    const response = await apiClient.post<MatchPlayerDto[]>(endpoints.matchPlayersAutofill(id));
    return response.data;
  },
  addRosterPlayer: async (id: number, payload: CreateMatchPlayerDto) => {
    const response = await apiClient.post<MatchPlayerDto>(endpoints.matchPlayers(id), payload);
    return response.data;
  },
  updateRosterPlayer: async (id: number, entryId: number, payload: UpdateMatchPlayerDto) => {
    const response = await apiClient.put<MatchPlayerDto>(endpoints.matchPlayer(id, entryId), payload);
    return response.data;
  },
  removeRosterPlayer: async (id: number, entryId: number) => {
    await apiClient.delete(endpoints.matchPlayer(id, entryId));
  }
};
