import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { MatchRatingDeltaDto, RatingChangeDto, RatingDto } from "../types";

export const ratingsApi = {
  getTeamRatings: async (teamId: number) => {
    const response = await apiClient.get<RatingDto[]>(endpoints.teamRatings(teamId));
    return response.data;
  },
  getTeamHistory: async (teamId: number, params?: { game?: string; take?: number }) => {
    const response = await apiClient.get<RatingChangeDto[]>(
      endpoints.teamRatingHistory(teamId),
      { params }
    );
    return response.data;
  },
  getPlayerRatings: async (playerId: number) => {
    const response = await apiClient.get<RatingDto[]>(endpoints.playerRatings(playerId));
    return response.data;
  },
  getPlayerHistory: async (playerId: number, params?: { game?: string; take?: number }) => {
    const response = await apiClient.get<RatingChangeDto[]>(
      endpoints.playerRatingHistory(playerId),
      { params }
    );
    return response.data;
  },
  getMatchDelta: async (matchId: number) => {
    const response = await apiClient.get<MatchRatingDeltaDto>(endpoints.matchRatingDelta(matchId));
    return response.data;
  }
};
