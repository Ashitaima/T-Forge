import { modelLabel } from "../../constants/predictionModels";
import type { TrainingRunDto, TrainingRunStatus } from "../../types";

const STATUS_LABELS: Record<TrainingRunStatus, string> = {
  Queued: "У черзі",
  Running: "Виконується",
  Succeeded: "Готово",
  Failed: "Збій"
};

// ember — лише на живому стані, за правилом єдиного акценту.
const STATUS_PILL: Record<TrainingRunStatus, string> = {
  Queued: "pill-neutral",
  Running: "pill-live",
  Succeeded: "pill-done",
  Failed: "pill-off"
};

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("uk-UA", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit"
  });

const duration = (run: TrainingRunDto) => {
  if (!run.startedAt) return "—";
  const end = run.finishedAt ? new Date(run.finishedAt) : new Date();
  const seconds = Math.max(0, Math.round((end.getTime() - new Date(run.startedAt).getTime()) / 1000));
  return seconds < 60 ? `${seconds} с` : `${Math.floor(seconds / 60)} хв ${seconds % 60} с`;
};

type Props = {
  runs: TrainingRunDto[];
  busyId: number | null;
  onActivate: (id: number) => void;
  onDelete: (id: number) => void;
};

export const TrainingRunsList = ({ runs, busyId, onActivate, onDelete }: Props) => (
  <div className="overflow-x-auto">
    <table className="table">
      <thead>
        <tr>
          <th>Датасет</th>
          <th>Стан</th>
          <th>Навчання / тест</th>
          <th>Log loss</th>
          <th>Час</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {runs.map((run) => (
          <tr key={run.id}>
            <td>
              <span className="block">{run.datasetName}</span>
              <span className="muted text-micro">
                {formatDateTime(run.queuedAt)} · {run.createdByUsername}
              </span>
            </td>
            <td>
              <span className={`pill ${STATUS_PILL[run.status]}`}>{STATUS_LABELS[run.status]}</span>
              {run.isActive && <span className="pill pill-done ml-1">Активна</span>}
              {run.errorMessage && (
                <span className="mt-1 block text-micro text-danger">{run.errorMessage}</span>
              )}
            </td>
            <td className="tabular">
              {run.rowsTrain !== null ? `${run.rowsTrain} / ${run.rowsTest}` : "—"}
            </td>
            <td className="tabular text-micro">
              {run.metrics.length === 0
                ? "—"
                : run.metrics.map((m) => `${modelLabel(m.model)} ${m.logLoss.toFixed(3)}`).join(" · ")}
            </td>
            <td className="tabular">{duration(run)}</td>
            <td className="text-right">
              {run.status === "Succeeded" && !run.isActive && (
                <button
                  className="btn btn-secondary btn-sm"
                  disabled={busyId !== null}
                  onClick={() => onActivate(run.id)}
                >
                  Активувати
                </button>
              )}
              {(run.status === "Succeeded" || run.status === "Failed") && !run.isActive && (
                <button
                  className="btn btn-ghost btn-sm ml-1"
                  disabled={busyId !== null}
                  onClick={() => onDelete(run.id)}
                >
                  Видалити
                </button>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  </div>
);
