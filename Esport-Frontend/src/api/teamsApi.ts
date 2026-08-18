import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type {
  TeamRowDto, CreateTeamDto, PagedResponse, TeamDto, TeamSummaryStatsDto, UpdateTeamDto } from "../types";

export const teamsApi = {
  getPaged: async (params: Record<string, string | number | boolean | undefined>) => {
    const response = await apiClient.get<PagedResponse<TeamRowDto>>(endpoints.teamsPaged, { params });
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
  transferCaptaincy: async (teamId: number, playerId: number) => {
    const response = await apiClient.put<TeamDto>(`${endpoints.teams}/${teamId}/captain`, { playerId });
    return response.data;
  },
  removePlayer: async (teamId: number, playerId: number) => {
    await apiClient.delete(`${endpoints.teams}/${teamId}/players/${playerId}`);
  },
  getSummary: async (id: number) => {
    const response = await apiClient.get<TeamSummaryStatsDto>(endpoints.teamSummary(id));
    return response.data;
  }
};
