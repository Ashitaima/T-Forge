import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { AuthResponseDto, UpdateProfileDto, UserDto } from "../types";

export const authApi = {
  login: async (payload: { username: string; password: string }) => {
    const response = await apiClient.post<AuthResponseDto>(endpoints.authLogin, payload);
    return response.data;
  },
  register: async (payload: {
    username: string;
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    role: string;
    nickname: string;
  }) => {
    const response = await apiClient.post<AuthResponseDto>(endpoints.authRegister, payload);
    return response.data;
  },
  profile: async () => {
    const response = await apiClient.get<UserDto>(endpoints.authProfile);
    return response.data;
  },
  updateProfile: async (payload: UpdateProfileDto) => {
    const response = await apiClient.put<UserDto>(endpoints.authProfile, payload);
    return response.data;
  },
  changePassword: async (payload: { currentPassword: string; newPassword: string }) => {
    await apiClient.post(endpoints.authChangePassword, payload);
  },
  logout: async () => {
    await apiClient.post(endpoints.authLogout);
  }
};
