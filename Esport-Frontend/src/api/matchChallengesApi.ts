import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { CreateMatchChallengeDto, MatchChallengeDto, MatchChallengeStatus } from "../types";

export const matchChallengesApi = {
  getForTeam: async (teamId: number, status?: MatchChallengeStatus) => {
    const response = await apiClient.get<MatchChallengeDto[]>(endpoints.teamMatchChallenges(teamId), {
      params: status ? { status } : undefined
    });
    return response.data;
  },
  getPending: async () => {
    const response = await apiClient.get<MatchChallengeDto[]>(endpoints.matchChallengesPending);
    return response.data;
  },
  create: async (payload: CreateMatchChallengeDto) => {
    const response = await apiClient.post<MatchChallengeDto>(endpoints.matchChallenges, payload);
    return response.data;
  },
  accept: async (id: number) => {
    const response = await apiClient.post<MatchChallengeDto>(endpoints.matchChallengeAccept(id));
    return response.data;
  },
  decline: async (id: number) => {
    const response = await apiClient.post<MatchChallengeDto>(endpoints.matchChallengeDecline(id));
    return response.data;
  },
  cancel: async (id: number) => {
    const response = await apiClient.post<MatchChallengeDto>(endpoints.matchChallengeCancel(id));
    return response.data;
  }
};
