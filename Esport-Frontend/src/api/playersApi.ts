import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { CreatePlayerDto, PagedResponse, PlayerDto, UpdatePlayerDto } from "../types";

export const playersApi = {
  getPaged: async (params: Record<string, string | number | boolean | undefined>) => {
    const response = await apiClient.get<PagedResponse<PlayerDto>>(endpoints.playersPaged, { params });
    return response.data;
  },
  getById: async (id: number) => {
    const response = await apiClient.get<PlayerDto>(`${endpoints.players}/${id}`);
    return response.data;
  },
  getWithTeam: async (id: number) => {
    const response = await apiClient.get<PlayerDto>(`${endpoints.players}/${id}/team`);
    return response.data;
  },
  getWithMatches: async (id: number) => {
    const response = await apiClient.get<PlayerDto>(`${endpoints.players}/${id}/matches`);
    return response.data;
  },
  create: async (payload: CreatePlayerDto) => {
    const response = await apiClient.post<PlayerDto>(endpoints.players, payload);
    return response.data;
  },
  update: async (id: number, payload: UpdatePlayerDto) => {
    const response = await apiClient.put<PlayerDto>(`${endpoints.players}/${id}`, payload);
    return response.data;
  },
  remove: async (id: number) => {
    await apiClient.delete(`${endpoints.players}/${id}`);
  },
  joinTeam: async (playerId: number, teamId: number) => {
    await apiClient.post(`${endpoints.players}/${playerId}/join-team/${teamId}`);
  },
  leaveTeam: async (playerId: number) => {
    await apiClient.post(`${endpoints.players}/${playerId}/leave-team`);
  }
};
