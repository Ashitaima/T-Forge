import { create } from "zustand";
import { authApi } from "../api/authApi";
import type { AuthResponseDto, UserDto } from "../types";

export type UserInfo = UserDto;

type AuthState = {
  token: string | null;
  user: UserInfo | null;
  isAuthenticated: boolean;
  setAuth: (payload: AuthResponseDto) => void;
  hydrate: () => void;
  logout: () => void;
  login: (payload: { username: string; password: string }) => Promise<void>;
  register: (payload: {
    username: string;
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    role: string;
  }) => Promise<void>;
};

export const authStore = create<AuthState>((set, get) => ({
  token: null,
  user: null,
  isAuthenticated: false,
  setAuth: (payload) => {
    localStorage.setItem("etm_token", payload.token);
    localStorage.setItem("etm_user", JSON.stringify(payload.user));
    localStorage.setItem("etm_expires", payload.expiresAt);
    set({ token: payload.token, user: payload.user, isAuthenticated: true });
  },
  hydrate: () => {
    const token = localStorage.getItem("etm_token");
    const userRaw = localStorage.getItem("etm_user");
    const user = userRaw ? (JSON.parse(userRaw) as UserInfo) : null;
    set({ token, user, isAuthenticated: Boolean(token) });
  },
  logout: () => {
    localStorage.removeItem("etm_token");
    localStorage.removeItem("etm_user");
    localStorage.removeItem("etm_expires");
    set({ token: null, user: null, isAuthenticated: false });
  },
  login: async ({ username, password }) => {
    const response = await authApi.login({ username, password });
    get().setAuth(response);
  },
  register: async (payload) => {
    const response = await authApi.register(payload);
    get().setAuth(response);
  }
}));

export const useAuthStore = authStore;
