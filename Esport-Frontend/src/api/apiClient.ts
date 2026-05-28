import axios from "axios";
import { authStore } from "../store/authStore";

const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json"
  }
});

apiClient.interceptors.request.use((config) => {
  const token = authStore.getState().token ?? localStorage.getItem("etm_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401) {
      authStore.getState().logout();
    }
    return Promise.reject(error);
  }
);
