import { useCallback, useEffect, useRef, useState } from "react";
import { mlApi } from "../../api/mlApi";
import { EmptyState, PageHeader, Skeleton } from "../../components/ui/Primitives";
import { useSubmitError } from "../../hooks/useSubmitError";
import type { ChessDatasetDto, ChessTrainingStatusDto, TrainingRunDto } from "../../types";
import { DatasetUploadForm } from "./DatasetUploadForm";
import { TrainingRunsList } from "./TrainingRunsList";

const POLL_MS = 2000;

const formatSize = (bytes: number) => `${(bytes / (1024 * 1024)).toFixed(1)} МБ`;

/**
 * Датасети й версії моделі прогнозу.
 *
 * Опитування, а не SignalR: навчання — довга задача без гранулярного
 * прогресу, і цикл на дві секунди переживає перезавантаження сторінки без
 * жодного хаба. Список теж не пагінований — датасетів і запусків одиниці,
 * тож usePagedList тут був би зайвим.
 */
const ChessModelsPage = () => {
  const [status, setStatus] = useState<ChessTrainingStatusDto | null>(null);
  const [datasets, setDatasets] = useState<ChessDatasetDto[]>([]);
  const [runs, setRuns] = useState<TrainingRunDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<number | null>(null);
  const actionError = useSubmitError();
  const timer = useRef<number | null>(null);

  const load = useCallback(async () => {
    const [nextStatus, nextDatasets, nextRuns] = await Promise.all([
      mlApi.status(),
      mlApi.datasets(),
      mlApi.runs()
    ]);
    setStatus(nextStatus);
    setDatasets(nextDatasets);
    setRuns(nextRuns);
    return nextRuns;
  }, []);

  useEffect(() => {
    let cancelled = false;

    const tick = async () => {
      try {
        const nextRuns = await load();
        if (cancelled) return;

        // Опитуємо лише поки є що чекати.
        if (nextRuns.some((run) => run.status === "Queued" || run.status === "Running")) {
          timer.current = window.setTimeout(tick, POLL_MS);
        }
      } catch {
        if (!cancelled) setStatus(null);
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    tick();

    return () => {
      cancelled = true;
      if (timer.current !== null) window.clearTimeout(timer.current);
    };
  }, [load]);

  const run = async (action: () => Promise<unknown>, id: number) => {
    actionError.clear();
    setBusyId(id);
    try {
      await action();
      await load();
    } catch (error) {
      actionError.capture(error, "Дія не виконалася.");
    } finally {
      setBusyId(null);
    }
  };

  const unfinished = runs.some((item) => item.status === "Queued" || item.status === "Running");

  if (loading) return <Skeleton />;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Прогнозування"
        title="Моделі прогнозу"
        description="Датасети й навчені версії"
      />

      {status && !status.available && (
        <div className="notice notice-error">Навчання недоступне: {status.reason}</div>
      )}

      {actionError.error && <div className="notice notice-error">{actionError.error}</div>}

      <section className="panel">
        <div className="panel-header flex items-baseline justify-between">
          <span className="eyebrow">Активна модель</span>
          {status?.activeRun && (
            <button
              className="btn btn-ghost btn-sm"
              disabled={busyId !== null}
              onClick={() => run(() => mlApi.activateBaseline(), 0)}
            >
              Повернути вбудовану
            </button>
          )}
        </div>
        <div className="panel-body">
          {status?.activeRun ? (
            <p>
              {status.activeRun.datasetName}{" "}
              <span className="muted text-micro">
                · навчено на {status.activeRun.rowsTrain?.toLocaleString("uk-UA")} партіях
              </span>
            </p>
          ) : (
            <p>
              {status?.baselineDataset} <span className="muted text-micro">· вбудована модель</span>
            </p>
          )}
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <span className="eyebrow">Датасети</span>
        </div>
        <DatasetUploadForm disabled={!status?.available} onUploaded={() => void load()} />
        {datasets.length === 0 ? (
          <EmptyState title="Ще немає датасетів" hint="Завантажте CSV із партіями, щоб навчити модель." />
        ) : (
          <div className="overflow-x-auto">
            <table className="table">
              <thead>
                <tr>
                  <th>Назва</th>
                  <th>Профіль</th>
                  <th>Партій</th>
                  <th>По контролю часу</th>
                  <th>Розмір</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {datasets.map((dataset) => (
                  <tr key={dataset.id}>
                    <td>
                      <span className="block">{dataset.name}</span>
                      <span className="muted text-micro">{dataset.originalFileName}</span>
                    </td>
                    <td>
                      <span className="pill pill-neutral">{dataset.profile}</span>
                      {!dataset.isTimeOrdered && (
                        <span className="mt-1 block text-micro text-text-muted">
                          без дати — поділ позиційний
                        </span>
                      )}
                    </td>
                    <td className="tabular">{dataset.rowCount.toLocaleString("uk-UA")}</td>
                    <td className="tabular text-micro">
                      {Object.entries(dataset.gamesByTimeControl)
                        .map(([control, count]) => `${control} ${count}`)
                        .join(" · ")}
                    </td>
                    <td className="tabular">{formatSize(dataset.sizeBytes)}</td>
                    <td className="text-right">
                      <button
                        className="btn btn-primary btn-sm"
                        disabled={!status?.available || unfinished || busyId !== null}
                        onClick={() => run(() => mlApi.queueRun(dataset.id), dataset.id)}
                      >
                        Навчити
                      </button>
                      {dataset.runCount === 0 && (
                        <button
                          className="btn btn-ghost btn-sm ml-1"
                          disabled={busyId !== null}
                          onClick={() => run(() => mlApi.deleteDataset(dataset.id), dataset.id)}
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
        )}
      </section>

      <section className="panel">
        <div className="panel-header">
          <span className="eyebrow">Навчання</span>
        </div>
        {runs.length === 0 ? (
          <EmptyState title="Навчання ще не запускали" />
        ) : (
          <TrainingRunsList
            runs={runs}
            busyId={busyId}
            onActivate={(id) => run(() => mlApi.activateRun(id), id)}
            onDelete={(id) => run(() => mlApi.deleteRun(id), id)}
          />
        )}
      </section>
    </div>
  );
};

export default ChessModelsPage;
