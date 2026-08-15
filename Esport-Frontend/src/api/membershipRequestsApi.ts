import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { MembershipRequestDto, MembershipRequestStatus } from "../types";

export const membershipRequestsApi = {
  getForTeam: async (teamId: number, status?: MembershipRequestStatus) => {
    const response = await apiClient.get<MembershipRequestDto[]>(
      endpoints.teamMembershipRequests(teamId),
      { params: status ? { status } : undefined }
    );
    return response.data;
  },
  getForPlayer: async (playerId: number, status?: MembershipRequestStatus) => {
    const response = await apiClient.get<MembershipRequestDto[]>(
      endpoints.playerMembershipRequests(playerId),
      { params: status ? { status } : undefined }
    );
    return response.data;
  },
  invite: async (teamId: number, playerId: number) => {
    const response = await apiClient.post<MembershipRequestDto>(
      endpoints.teamInvitations(teamId, playerId)
    );
    return response.data;
  },
  apply: async (playerId: number, teamId: number) => {
    const response = await apiClient.post<MembershipRequestDto>(
      endpoints.playerApplications(playerId, teamId)
    );
    return response.data;
  },
  accept: async (id: number) => {
    const response = await apiClient.post<MembershipRequestDto>(
      endpoints.membershipRequestAccept(id)
    );
    return response.data;
  },
  decline: async (id: number) => {
    const response = await apiClient.post<MembershipRequestDto>(
      endpoints.membershipRequestDecline(id)
    );
    return response.data;
  },
  cancel: async (id: number) => {
    const response = await apiClient.post<MembershipRequestDto>(
      endpoints.membershipRequestCancel(id)
    );
    return response.data;
  }
};
