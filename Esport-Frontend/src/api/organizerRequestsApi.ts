import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { OrganizerRequestDto, OrganizerRequestStatus } from "../types";

export const organizerRequestsApi = {
  apply: async (message: string) => {
    const response = await apiClient.post<OrganizerRequestDto>(endpoints.organizerRequests, {
      message
    });
    return response.data;
  },
  getMine: async () => {
    const response = await apiClient.get<OrganizerRequestDto[]>(endpoints.organizerRequestsMine);
    return response.data;
  },
  /** Черга адміністратора. Решті ролей ендпойнт відповідає 403. */
  getAll: async (status?: OrganizerRequestStatus) => {
    const response = await apiClient.get<OrganizerRequestDto[]>(endpoints.organizerRequests, {
      params: status ? { status } : undefined
    });
    return response.data;
  },
  approve: async (id: number) => {
    const response = await apiClient.post<OrganizerRequestDto>(endpoints.organizerRequestApprove(id));
    return response.data;
  },
  decline: async (id: number, responseNote: string) => {
    const response = await apiClient.post<OrganizerRequestDto>(
      endpoints.organizerRequestDecline(id),
      { responseNote }
    );
    return response.data;
  },
  cancel: async (id: number) => {
    const response = await apiClient.post<OrganizerRequestDto>(endpoints.organizerRequestCancel(id));
    return response.data;
  }
};
