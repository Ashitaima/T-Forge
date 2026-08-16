import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type {
  CreateFullPlayerDto,
  CreatePlayerDto,
  PagedResponse,
  PlayerDto,
  PlayerMatchDto,
  PlayerProfileDto,
  PlayerRowDto,
  UpdatePlayerDto
} from "../types";

export const playersApi = {
  getPaged: async (params: Record<string, string | number | boolean | undefined>) => {
    const response = await apiClient.get<PagedResponse<PlayerRowDto>>(endpoints.playersPaged, { params });
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
  getProfile: async (id: number) => {
    const response = await apiClient.get<PlayerProfileDto>(endpoints.playerProfile(id));
    return response.data;
  },
  getMatchLog: async (id: number, params: { page: number; pageSize: number }) => {
    const response = await apiClient.get<PagedResponse<PlayerMatchDto>>(endpoints.playerMatches(id), { params });
    return response.data;
  },
  create: async (payload: CreatePlayerDto) => {
    const response = await apiClient.post<PlayerDto>(endpoints.players, payload);
    return response.data;
  },
  /** Створення облікового запису разом із профілем. Лише для адміністратора. */
  createFull: async (payload: CreateFullPlayerDto) => {
    const response = await apiClient.post<PlayerDto>(endpoints.playersFull, payload);
    return response.data;
  },
  update: async (id: number, payload: UpdatePlayerDto) => {
    const response = await apiClient.put<PlayerDto>(`${endpoints.players}/${id}`, payload);
    return response.data;
  },
  remove: async (id: number) => {
    await apiClient.delete(`${endpoints.players}/${id}`);
  },
  leaveTeam: async (playerId: number) => {
    await apiClient.post(`${endpoints.players}/${playerId}/leave-team`);
  },
  getMe: async () => {
    const response = await apiClient.get<PlayerDto>(endpoints.playersMe);
    return response.data;
  }
};
