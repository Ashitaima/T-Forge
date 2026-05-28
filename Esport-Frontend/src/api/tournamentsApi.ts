import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { CreateTournamentDto, PagedResponse, TournamentDto, TournamentStatsDto, UpdateTournamentDto } from "../types";

export const tournamentsApi = {
  getAllActive: async () => {
    const response = await apiClient.get<TournamentDto[]>(endpoints.tournaments);
    return response.data;
  },
  getPaged: async (params: Record<string, string | number | boolean | undefined>) => {
    const response = await apiClient.get<PagedResponse<TournamentDto>>(endpoints.tournamentsPaged, { params });
    return response.data;
  },
  getById: async (id: number) => {
    const response = await apiClient.get<TournamentDto>(`${endpoints.tournaments}/${id}`);
    return response.data;
  },
  getStats: async () => {
    const response = await apiClient.get<TournamentStatsDto>(endpoints.tournamentsStats);
    return response.data;
  },
  create: async (payload: CreateTournamentDto) => {
    const response = await apiClient.post<TournamentDto>(endpoints.tournaments, payload);
    return response.data;
  },
  update: async (id: number, payload: UpdateTournamentDto) => {
    const response = await apiClient.put<TournamentDto>(`${endpoints.tournaments}/${id}`, payload);
    return response.data;
  },
  remove: async (id: number) => {
    await apiClient.delete(`${endpoints.tournaments}/${id}`);
  }
};
