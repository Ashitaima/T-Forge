import axios from "axios";
import { authStore } from "../store/authStore";

const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

// Content-Type тут навмисно не задано. Axios сам ставить application/json для
// звичайного обʼєкта, а для FormData — multipart разом із boundary. Постійний
// заголовок application/json ламав саме друге: побачивши його, axios
// перетворював FormData на JSON, і файл аватара доходив до сервера як текст,
// тобто не доходив зовсім.
export const apiClient = axios.create({
  baseURL: API_BASE_URL
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
