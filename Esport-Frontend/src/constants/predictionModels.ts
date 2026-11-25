/**
 * Назви моделей прогнозу — дзеркало Esport-Backend/Common/PredictionModels.cs.
 *
 * Жили в PredictionPanel.tsx локально, поки їх читала одна панель. Таблиця
 * запусків навчання показує ті самі моделі, тож підписи переїхали в константи:
 * друга копія цього словника розійшлася б із першою.
 */
export const MODEL_LABELS: Record<string, string> = {
  LogisticRegression: "Логістична регресія",
  GradientBoosting: "Градієнтний бустинг",
  Elo: "Ело (базова лінія)"
};

export const modelLabel = (model: string) => MODEL_LABELS[model] ?? model;
