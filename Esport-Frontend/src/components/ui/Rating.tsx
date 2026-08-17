import { gameLabel } from "../../constants/games";
import { tierClass, tierLabel } from "../../constants/ratingTiers";
import type { RatingChangeDto } from "../../types";

/** Ліга як стримана шкала: акцент лишається за діями та живим станом. */
export const TierBadge = ({ tier }: { tier: string | null | undefined }) => {
  if (!tier) {
    return null;
  }

  return <span className={`pill ${tierClass(tier)}`}>{tierLabel(tier)}</span>;
};

/**
 * Рейтинг у рядку таблиці. Без турнірних матчів рейтингу немає взагалі —
 * показуємо прочерк, а не вигадану тисячу.
 */
export const RatingCell = ({
  rating,
  game
}: {
  rating: number | null;
  game?: string | null;
}) => {
  if (rating == null) {
    return <span className="text-text-faint">—</span>;
  }

  return (
    <span className="tabular font-mono" title={game ? gameLabel(game) : undefined}>
      {rating}
    </span>
  );
};

/** Приріст за матч: знак несе сенс, тож він завжди видимий. */
export const RatingDelta = ({ delta }: { delta: number | null | undefined }) => {
  if (delta == null) {
    return null;
  }

  return (
    <span
      className={`tabular font-mono text-micro ${
        delta > 0 ? "text-win" : delta < 0 ? "text-danger" : "text-text-faint"
      }`}
      title="Зміна рейтингу за цей матч"
    >
      {delta > 0 ? `+${delta}` : delta}
    </span>
  );
};

/**
 * Графік рейтингу за останні матчі.
 *
 * Інлайновий SVG замість бібліотеки: тут одна ламана без осей, легенди та
 * взаємодії — цілої залежності вона не варта. Форма важливіша за точні
 * значення, самі числа стоять поруч у картках і в журналі.
 */
export const RatingSparkline = ({
  history,
  className = ""
}: {
  history: RatingChangeDto[];
  className?: string;
}) => {
  // Одна точка — це не тренд: лінію нема між чим провести.
  if (history.length < 2) {
    return null;
  }

  const width = 100;
  const height = 32;
  const padding = 2;

  const values = history.map((entry) => entry.ratingAfter);
  const min = Math.min(...values);
  const max = Math.max(...values);
  // Пласка історія (усі значення однакові) інакше дала б ділення на нуль.
  const span = max - min || 1;

  const points = values.map((value, index) => {
    const x = padding + (index / (values.length - 1)) * (width - padding * 2);
    const y = padding + (1 - (value - min) / span) * (height - padding * 2);
    return `${x.toFixed(2)},${y.toFixed(2)}`;
  });

  const rising = values[values.length - 1] >= values[0];
  const stroke = rising ? "#4ADE80" : "#F4574D";

  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      preserveAspectRatio="none"
      className={`h-8 w-full ${className}`}
      role="img"
      aria-label={`Рейтинг за останні ${values.length} матчів: від ${values[0]} до ${
        values[values.length - 1]
      }`}
    >
      {/* Заливка під лінією читається як обсяг, тож тримаємо її ледь помітною */}
      <polygon
        points={`${padding},${height} ${points.join(" ")} ${width - padding},${height}`}
        fill={stroke}
        opacity={0.1}
      />
      <polyline
        points={points.join(" ")}
        fill="none"
        stroke={stroke}
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
        vectorEffect="non-scaling-stroke"
      />
    </svg>
  );
};

/**
 * Рейтинг за дисциплінами — блок для профілю гравця чи команди.
 * Порожній список означає «жодного турнірного матчу», і про це варто сказати
 * прямо: товариські матчі рейтинг не змінюють, і це навмисно.
 */
export const RatingPanel = ({
  ratings,
  history,
  emptyHint
}: {
  ratings: { game: string; rating: number; peak: number; matchesRated: number; tier: string }[];
  history: RatingChangeDto[];
  emptyHint: string;
}) => {
  if (ratings.length === 0) {
    return (
      <p className="muted text-body">
        Рейтингу ще немає. {emptyHint}
      </p>
    );
  }

  return (
    <div className="space-y-5">
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {ratings.map((entry) => (
          <div key={entry.game} className="surface-raised px-4 py-3.5">
            <div className="flex items-center justify-between gap-2">
              <span className="eyebrow">{gameLabel(entry.game)}</span>
              <TierBadge tier={entry.tier} />
            </div>
            <div className="tabular mt-2.5 font-mono text-[1.75rem] font-medium leading-none text-text">
              {entry.rating}
            </div>
            <div className="muted mt-2 font-mono text-micro">
              пік {entry.peak} · матчів {entry.matchesRated}
            </div>
          </div>
        ))}
      </div>

      {history.length >= 2 && (
        <div>
          <div className="eyebrow mb-2">Останні {history.length} матчів</div>
          <RatingSparkline history={history} />
        </div>
      )}
    </div>
  );
};
