import { useEffect, useState } from "react";
import { predictionsApi } from "../../api/predictionsApi";
import { timeControlLabel } from "../../constants/timeControls";
import type {
  DuelPredictionDto,
  MatchPredictionDto,
  ModelOutcomeDto,
  PredictionModelInfoDto
} from "../../types";

/**
 * Прогноз шахових партій двома моделями й базовою лінією Ело
 * (docs/superpowers/specs/2026-11-05-chess-discipline-and-prediction-design.md).
 *
 * Шанс першої сторони — зеленим (win), другої — червоним (danger), нічия —
 * нейтральна. Ember не використовується: прогноз — інформація, а не дія.
 * Ключі — Esport-Backend/Common/PredictionModels.cs.
 */
const MODEL_LABELS: Record<string, string> = {
  LogisticRegression: "Логістична регресія",
  GradientBoosting: "Градієнтний бустинг",
  Elo: "Ело (базова лінія)"
};

const modelLabel = (model: string) => MODEL_LABELS[model] ?? model;
const pct = (value: number) => `${Math.round(value * 100)}%`;

/** Підпис усередині сегмента — лише якщо він достатньо широкий, щоб текст не вилазив. */
const Segment = ({ value, className }: { value: number; className: string }) => (
  <div className={`flex items-center justify-center ${className}`} style={{ width: `${value * 100}%` }}>
    {value >= 0.12 && pct(value)}
  </div>
);

/** Одна смуга: перемога першого / нічия / перемога другого. */
const OutcomeBar = ({ outcome }: { outcome: ModelOutcomeDto }) => (
  <div className="grid grid-cols-[minmax(8rem,10rem)_1fr] items-center gap-3">
    <span className="text-micro text-text-muted">{modelLabel(outcome.model)}</span>
    <div
      className="flex h-5 overflow-hidden rounded border border-line font-mono text-micro tabular text-text"
      title={`${pct(outcome.firstWin)} · нічия ${pct(outcome.draw)} · ${pct(outcome.secondWin)}`}
    >
      <Segment value={outcome.firstWin} className="bg-win/35" />
      <Segment value={outcome.draw} className="bg-ink-700 text-text-muted" />
      <Segment value={outcome.secondWin} className="bg-danger/35" />
    </div>
  </div>
);

const ModelNote = ({ info, littleData }: { info: PredictionModelInfoDto | null; littleData: boolean }) => (
  <div className="mt-3 space-y-1 text-micro">
    {littleData && (
      <p className="text-text-muted">
        Мало даних для цього контролю часу: моделі навчено переважно на рапіді й класиці, тож це
        екстраполяція.
      </p>
    )}
    {info && (
      <p className="muted">
        Навчено на {info.rowsTrain.toLocaleString("uk-UA")} партіях ({info.dataset}). Log loss на тесті:{" "}
        {info.metrics.map((m) => `${modelLabel(m.model)} ${m.logLoss.toFixed(3)}`).join(" · ")}
      </p>
    )}
  </div>
);

const Header = ({ first, second, timeControl }: { first: string; second: string; timeControl: string | null }) => (
  <div className="mb-3 flex flex-wrap items-baseline justify-between gap-2">
    <span className="eyebrow">Прогноз · {timeControlLabel(timeControl)}</span>
    <span className="flex flex-wrap items-center gap-x-3 gap-y-1 text-micro text-text-muted">
      <span className="flex items-center gap-1.5">
        <span className="h-2 w-2 rounded-sm bg-win/70" />
        {first}
      </span>
      <span className="flex items-center gap-1.5">
        <span className="h-2 w-2 rounded-sm bg-ink-700 ring-1 ring-line" />
        нічия
      </span>
      <span className="flex items-center gap-1.5">
        <span className="h-2 w-2 rounded-sm bg-danger/70" />
        {second}
      </span>
    </span>
  </div>
);

/** Завантажує прогноз один раз на id; помилка мережі просто ховає блок. */
const usePrediction = <T,>(id: number, load: (id: number) => Promise<T>) => {
  const [data, setData] = useState<T | null>(null);

  useEffect(() => {
    let alive = true;
    load(id)
      .then((result) => alive && setData(result))
      .catch(() => alive && setData(null));
    return () => {
      alive = false;
    };
  }, [id, load]);

  return data;
};

type DuelProps = { duelId: number; firstName: string; secondName: string };

export const DuelPrediction = ({ duelId, firstName, secondName }: DuelProps) => {
  const data = usePrediction<DuelPredictionDto>(duelId, predictionsApi.duel);

  if (!data) return null;
  if (!data.available) {
    return <p className="muted text-micro">Прогноз недоступний: {data.reason}</p>;
  }

  return (
    <div className="surface-raised rounded p-3">
      <Header first={firstName} second={secondName} timeControl={data.timeControl} />
      <div className="space-y-2">
        {data.outcomes.map((o) => (
          <OutcomeBar key={o.model} outcome={o} />
        ))}
      </div>
      {data.whitePlayerId === null && (
        <p className="muted mt-2 text-micro">Стартовий колір гравців ще не визначено — прогноз усереднює обидва варіанти.</p>
      )}
      <ModelNote info={data.modelInfo} littleData={data.littleData} />
    </div>
  );
};

type MatchProps = { matchId: number; homeName: string; awayName: string };

export const MatchPrediction = ({ matchId, homeName, awayName }: MatchProps) => {
  const data = usePrediction<MatchPredictionDto>(matchId, predictionsApi.match);

  if (!data) return null;

  return (
    <section className="panel">
      <div className="panel-body">
        {!data.available ? (
          <p className="muted text-micro">Прогноз недоступний: {data.reason}</p>
        ) : (
          <>
            <Header first={homeName} second={awayName} timeControl={data.timeControl} />
            <div className="overflow-x-auto">
              <table className="table">
                <thead>
                  <tr>
                    <th>Очікувані очки</th>
                    <th className="tabular">{homeName}</th>
                    <th className="tabular">{awayName}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.totals.map((t) => (
                    <tr key={t.model}>
                      <td>{modelLabel(t.model)}</td>
                      <td className="tabular font-mono">{t.home.toFixed(1)}</td>
                      <td className="tabular font-mono">{t.away.toFixed(1)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="mt-4 space-y-4">
              {data.boards.map((b) => (
                <div key={b.board}>
                  <p className="mb-1 text-micro text-text-muted">
                    Дошка {b.board}: {b.homeNickname}{" "}
                    <span className="font-mono tabular">({b.homeRating})</span>{" "}
                    {b.homeIsWhite ? "білими" : "чорними"} — {b.awayNickname}{" "}
                    <span className="font-mono tabular">({b.awayRating})</span>
                  </p>
                  <div className="space-y-1">
                    {b.outcomes.map((o) => (
                      <OutcomeBar key={o.model} outcome={o} />
                    ))}
                  </div>
                </div>
              ))}
            </div>
            <ModelNote info={data.modelInfo} littleData={data.littleData} />
          </>
        )}
      </div>
    </section>
  );
};
