import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { PagedResponse, UserDto } from "../types";

export const usersApi = {
  getPaged: async (params: Record<string, string | number | boolean | undefined>) => {
    const response = await apiClient.get<PagedResponse<UserDto>>(endpoints.usersPaged, { params });
    return response.data;
  },
  getById: async (id: number) => {
    const response = await apiClient.get<UserDto>(`${endpoints.users}/${id}`);
    return response.data;
  },
  create: async (payload: { username: string; email: string; password: string; firstName: string; lastName: string; role: string }) => {
    const response = await apiClient.post<UserDto>(endpoints.users, payload);
    return response.data;
  },
  update: async (id: number, payload: { firstName: string; lastName: string; email: string }) => {
    const response = await apiClient.put<UserDto>(`${endpoints.users}/${id}`, payload);
    return response.data;
  },
  remove: async (id: number) => {
    await apiClient.delete(`${endpoints.users}/${id}`);
  },
  activate: async (id: number) => {
    await apiClient.post(`${endpoints.users}/${id}/activate`);
  },
  deactivate: async (id: number) => {
    await apiClient.post(`${endpoints.users}/${id}/deactivate`);
  }
};
