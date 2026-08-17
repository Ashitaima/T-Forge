import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { TournamentInvitationDto, TournamentInvitationStatus } from "../types";

export const tournamentInvitationsApi = {
  getForTournament: async (tournamentId: number, status?: TournamentInvitationStatus) => {
    const response = await apiClient.get<TournamentInvitationDto[]>(
      endpoints.tournamentInvitations(tournamentId),
      { params: status ? { status } : undefined }
    );
    return response.data;
  },
  getForTeam: async (teamId: number, status?: TournamentInvitationStatus) => {
    const response = await apiClient.get<TournamentInvitationDto[]>(
      endpoints.teamTournamentInvitations(teamId),
      { params: status ? { status } : undefined }
    );
    return response.data;
  },
  /** Організатор запрошує команду. */
  invite: async (tournamentId: number, teamId: number, message = "") => {
    const response = await apiClient.post<TournamentInvitationDto>(
      endpoints.tournamentInvite(tournamentId, teamId),
      { message }
    );
    return response.data;
  },
  /** Капітан подає заявку на участь своєї команди. */
  apply: async (tournamentId: number, teamId: number, message = "") => {
    const response = await apiClient.post<TournamentInvitationDto>(
      endpoints.tournamentApplication(tournamentId, teamId),
      { message }
    );
    return response.data;
  },
  accept: async (id: number) => {
    const response = await apiClient.post<TournamentInvitationDto>(
      endpoints.tournamentInvitationAccept(id)
    );
    return response.data;
  },
  decline: async (id: number) => {
    const response = await apiClient.post<TournamentInvitationDto>(
      endpoints.tournamentInvitationDecline(id)
    );
    return response.data;
  },
  cancel: async (id: number) => {
    const response = await apiClient.post<TournamentInvitationDto>(
      endpoints.tournamentInvitationCancel(id)
    );
    return response.data;
  }
};
