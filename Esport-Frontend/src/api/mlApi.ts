import { apiClient } from "./apiClient";
import { endpoints } from "./endpoints";
import type { ChessDatasetDto, ChessTrainingStatusDto, TrainingRunDto } from "../types";

/** Датасети й версії моделей прогнозу. Усе — лише для адміністратора. */
export const mlApi = {
  status: async () =>
    (await apiClient.get<ChessTrainingStatusDto>(endpoints.mlStatus)).data,

  datasets: async () =>
    (await apiClient.get<ChessDatasetDto[]>(endpoints.mlDatasets)).data,

  /** multipart: Content-Type ставить браузер сам, разом із boundary. */
  uploadDataset: async (name: string, file: File) => {
    const form = new FormData();
    form.append("name", name);
    form.append("file", file);
    return (await apiClient.post<ChessDatasetDto>(endpoints.mlDatasets, form)).data;
  },

  deleteDataset: async (id: number) => {
    await apiClient.delete(endpoints.mlDataset(id));
  },

  runs: async () => (await apiClient.get<TrainingRunDto[]>(endpoints.mlRuns)).data,

  run: async (id: number) => (await apiClient.get<TrainingRunDto>(endpoints.mlRun(id))).data,

  queueRun: async (datasetId: number) =>
    (await apiClient.post<TrainingRunDto>(endpoints.mlRuns, { datasetId })).data,

  activateRun: async (id: number) =>
    (await apiClient.post<TrainingRunDto>(endpoints.mlRunActivate(id))).data,

  activateBaseline: async () =>
    (await apiClient.post<ChessTrainingStatusDto>(endpoints.mlBaselineActivate)).data,

  deleteRun: async (id: number) => {
    await apiClient.delete(endpoints.mlRun(id));
  }
};
