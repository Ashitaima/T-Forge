import { create } from "zustand";
import { authApi } from "../api/authApi";
import type { AuthResponseDto, UserDto } from "../types";

export type UserInfo = UserDto;

type AuthState = {
  token: string | null;
  user: UserInfo | null;
  isAuthenticated: boolean;
  /** Режим розробника: роль, під якою адміністратор дивиться інтерфейс. */
  previewRole: string;
  /**
   * Режим розробника: команда, капітаном якої адміністратор себе бачить.
   * Капітанство — це не роль, а звʼязок Team.CaptainId, тож рольовий
   * перемикач його виразити не може: потрібна конкретна команда.
   * 0 — підміни немає.
   */
  previewCaptainTeamId: number;
  setAuth: (payload: AuthResponseDto) => void;
  setUser: (user: UserInfo) => void;
  setPreviewRole: (role: string) => void;
  setPreviewCaptainTeamId: (teamId: number) => void;
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
    nickname: string;
  }) => Promise<void>;
};

export const authStore = create<AuthState>((set, get) => ({
  token: null,
  user: null,
  isAuthenticated: false,
  previewRole: "",
  previewCaptainTeamId: 0,
  setAuth: (payload) => {
    localStorage.setItem("etm_token", payload.token);
    localStorage.setItem("etm_user", JSON.stringify(payload.user));
    localStorage.setItem("etm_expires", payload.expiresAt);
    set({ token: payload.token, user: payload.user, isAuthenticated: true });
  },
  setUser: (user) => {
    localStorage.setItem("etm_user", JSON.stringify(user));
    set({ user });
  },
  // Режим розробника підміняє роль лише в інтерфейсі. Токен, який іде до API,
  // залишається справжнім — підмінити права на сервері звідси неможливо.
  setPreviewRole: (role) => set({ previewRole: role }),
  // Так само лише в інтерфейсі: запити до API йдуть від справжнього
  // адміністратора, у якого права на ці дії й так є.
  setPreviewCaptainTeamId: (teamId) => set({ previewCaptainTeamId: teamId }),
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
    set({
      token: null,
      user: null,
      isAuthenticated: false,
      previewRole: "",
      previewCaptainTeamId: 0
    });
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
