import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { CreateTeamDto, PagedResponse, TeamDto, UpdateTeamDto } from "../types";

export const teamsApi = {
  getPaged: async (params: Record<string, string | number | boolean | undefined>) => {
    const response = await apiClient.get<PagedResponse<TeamDto>>(endpoints.teamsPaged, { params });
    return response.data;
  },
  getById: async (id: number) => {
    const response = await apiClient.get<TeamDto>(`${endpoints.teams}/${id}`);
    return response.data;
  },
  getWithPlayers: async (id: number) => {
    const response = await apiClient.get<TeamDto>(`${endpoints.teams}/${id}/players`);
    return response.data;
  },
  create: async (payload: CreateTeamDto) => {
    const response = await apiClient.post<TeamDto>(endpoints.teams, payload);
    return response.data;
  },
  update: async (id: number, payload: UpdateTeamDto) => {
    const response = await apiClient.put<TeamDto>(`${endpoints.teams}/${id}`, payload);
    return response.data;
  },
  remove: async (id: number) => {
    await apiClient.delete(`${endpoints.teams}/${id}`);
  },
  addPlayer: async (teamId: number, playerId: number) => {
    await apiClient.post(`${endpoints.teams}/${teamId}/players/${playerId}`);
  },
  removePlayer: async (teamId: number, playerId: number) => {
    await apiClient.delete(`${endpoints.teams}/${teamId}/players/${playerId}`);
  }
};
