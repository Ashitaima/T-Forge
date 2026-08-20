import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { NotificationDto } from "../types";

export const notificationsApi = {
  list: async () => {
    const response = await apiClient.get<NotificationDto[]>(endpoints.notifications);
    return response.data;
  },
  unreadCount: async () => {
    const response = await apiClient.get<number>(endpoints.notificationsUnreadCount);
    return response.data;
  },
  markSeen: async () => {
    await apiClient.post(endpoints.notificationsSeen);
  }
};
