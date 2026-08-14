import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { PlayerStandingDto, TeamStandingDto } from "../types";

export const standingsApi = {
  getTeams: async () => {
    const response = await apiClient.get<TeamStandingDto[]>(endpoints.standingsTeams);
    return response.data;
  },
  getPlayers: async () => {
    const response = await apiClient.get<PlayerStandingDto[]>(endpoints.standingsPlayers);
    return response.data;
  }
};
