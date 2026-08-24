import { apiClient } from "./apiClient";
import type { PagedResponse, PlayerSummaryDto, SwipeResultDto, TeamOpeningDto } from "../types";

export type CreateTeamOpeningPayload = {
  teamId: number;
  game: string;
  position: string;
  minRank: string;
  minUtcOffsetMinutes: number | null;
  maxUtcOffsetMinutes: number | null;
  pitch: string;
};

export type UpdateTeamOpeningPayload = Omit<CreateTeamOpeningPayload, "teamId" | "game">;

export const lftApi = {
  openingFeed: (page: number, pageSize: number) =>
    apiClient
      .get<PagedResponse<TeamOpeningDto>>("/api/lft/openings", { params: { page, pageSize } })
      .then((response) => response.data),

  candidateFeed: (openingId: number, page: number, pageSize: number) =>
    apiClient
      .get<PagedResponse<PlayerSummaryDto>>("/api/lft/candidates", {
        params: { openingId, page, pageSize }
      })
      .then((response) => response.data),

  swipe: (teamOpeningId: number, like: boolean, playerId?: number) =>
    apiClient
      .post<SwipeResultDto>("/api/lft/swipe", { teamOpeningId, like, playerId })
      .then((response) => response.data),

  openingsForTeam: (teamId: number) =>
    apiClient
      .get<TeamOpeningDto[]>(`/api/teams/${teamId}/openings`)
      .then((response) => response.data),

  createOpening: (payload: CreateTeamOpeningPayload) =>
    apiClient.post<TeamOpeningDto>("/api/team-openings", payload).then((response) => response.data),

  updateOpening: (id: number, payload: UpdateTeamOpeningPayload) =>
    apiClient.put<TeamOpeningDto>(`/api/team-openings/${id}`, payload).then((response) => response.data),

  closeOpening: (id: number) =>
    apiClient.post<TeamOpeningDto>(`/api/team-openings/${id}/close`, {}).then((response) => response.data)
};
