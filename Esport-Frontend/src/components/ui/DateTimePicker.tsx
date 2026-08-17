import { useEffect, useMemo, useRef, useState } from "react";
import { CalendarDays, ChevronLeft, ChevronRight, Clock } from "lucide-react";

/**
 * Вибір дати й часу.
 *
 * Нативний input[type=datetime-local] показує маску «дд.мм.рррр --:--», у якій
 * доводиться потрапляти курсором в окремі сегменти й друкувати цифри наосліп.
 * Тут замість цього календар, у якому день видно, і час, зібраний із двох
 * коротких списків — година та хвилина з кроком у чверть. Клавіатурний ввід
 * нікуди не подівся: поле згори лишається текстовим.
 *
 * Значення назовні — той самий рядок, що й у нативних полів: «YYYY-MM-DDTHH:mm»
 * у режимі datetime і «YYYY-MM-DD» у режимі date. Форми, що на них спираються,
 * не змінюються.
 */

const WEEKDAYS = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд"];

const MONTHS = [
  "Січень", "Лютий", "Березень", "Квітень", "Травень", "Червень",
  "Липень", "Серпень", "Вересень", "Жовтень", "Листопад", "Грудень"
];

const HOURS = Array.from({ length: 24 }, (_, hour) => hour);
const MINUTES = [0, 15, 30, 45];

const pad = (value: number) => String(value).padStart(2, "0");

/** Локальна дата як «YYYY-MM-DD» — без toISOString, який зсуває на UTC. */
const toDateKey = (date: Date) =>
  `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;

const parseValue = (value: string) => {
  const match = /^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2}))?/.exec(value.trim());
  if (!match) {
    return null;
  }

  const [, year, month, day, hour, minute] = match;
  return {
    date: new Date(Number(year), Number(month) - 1, Number(day)),
    hour: hour === undefined ? null : Number(hour),
    minute: minute === undefined ? null : Number(minute)
  };
};

/** Понеділок як перший день тижня — так тут читають календар. */
const startOfGrid = (month: Date) => {
  const first = new Date(month.getFullYear(), month.getMonth(), 1);
  const shift = (first.getDay() + 6) % 7;
  return new Date(first.getFullYear(), first.getMonth(), 1 - shift);
};

type Props = {
  value: string;
  onChange: (value: string) => void;
  /** «datetime» — дата й час, «date» — лише дата. */
  mode?: "datetime" | "date";
  /** Дати раніше за цю вибрати не можна. Формат «YYYY-MM-DD». */
  minDate?: string;
  id?: string;
  ariaLabel?: string;
};

export const DateTimePicker = ({
  value,
  onChange,
  mode = "datetime",
  minDate,
  id,
  ariaLabel
}: Props) => {
  const withTime = mode === "datetime";
  const parsed = parseValue(value);

  const [open, setOpen] = useState(false);
  const [month, setMonth] = useState(() => parsed?.date ?? new Date());
  const containerRef = useRef<HTMLDivElement>(null);

  // Календар іде за значенням: набрали дату вручну — гортається сам.
  // У залежностях сам рядок, а не Date: об'єкт щоразу новий і зациклив би ефект.
  useEffect(() => {
    const next = parseValue(value);
    if (!next) {
      return;
    }

    setMonth((current) =>
      current.getFullYear() === next.date.getFullYear() &&
      current.getMonth() === next.date.getMonth()
        ? current
        : next.date
    );
  }, [value]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const onPointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);

    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  const days = useMemo(() => {
    const start = startOfGrid(month);
    return Array.from({ length: 42 }, (_, index) => {
      const day = new Date(start);
      day.setDate(start.getDate() + index);
      return day;
    });
  }, [month]);

  const selectedKey = parsed ? toDateKey(parsed.date) : null;
  const todayKey = toDateKey(new Date());

  // Типовий час — вечір: матчі призначають саме на нього, тож вибір дати
  // без часу одразу дає осмислене значення, а не опівніч.
  const hour = parsed?.hour ?? 18;
  const minute = parsed?.minute ?? 0;

  const emit = (date: Date, nextHour: number, nextMinute: number) =>
    onChange(
      withTime ? `${toDateKey(date)}T${pad(nextHour)}:${pad(nextMinute)}` : toDateKey(date)
    );

  const pickDate = (day: Date) => {
    emit(day, hour, minute);
    if (!withTime) {
      setOpen(false); // у режимі дати вибір завершено одним кліком
    }
  };

  // Час без дати нічого не означає — якщо її ще нема, беремо сьогодні.
  const pickTime = (nextHour: number, nextMinute: number) =>
    emit(parsed?.date ?? new Date(), nextHour, nextMinute);

  const isDisabled = (day: Date) => Boolean(minDate) && toDateKey(day) < minDate!;

  const summary = parsed
    ? [
        parsed.date.toLocaleDateString("uk-UA", { day: "2-digit", month: "long", year: "numeric" }),
        withTime ? `${pad(hour)}:${pad(minute)}` : null
      ]
        .filter(Boolean)
        .join(", ")
    : "";

  const shiftMonth = (delta: number) =>
    setMonth(new Date(month.getFullYear(), month.getMonth() + delta, 1));

  return (
    <div className="relative" ref={containerRef}>
      <div className="relative">
        {/* Поле лишається текстовим: набрати дату з клавіатури швидше, ніж
            клікати, і відбирати цю можливість не варто. */}
        <input
          id={id}
          type="text"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          onFocus={() => setOpen(true)}
          placeholder={withTime ? "РРРР-ММ-ДД ГГ:ХХ" : "РРРР-ММ-ДД"}
          aria-label={ariaLabel}
          autoComplete="off"
          className="input pr-10 font-mono"
        />
        <button
          type="button"
          onClick={() => setOpen((current) => !current)}
          aria-label={open ? "Сховати календар" : "Показати календар"}
          aria-expanded={open}
          className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-text-faint transition hover:bg-ink-800 hover:text-text"
        >
          <CalendarDays className="h-4 w-4" />
        </button>
      </div>

      {summary && !open && <p className="field-hint">{summary}</p>}

      {open && (
        <div className="surface-raised absolute left-0 z-30 mt-2 w-[19rem] p-4">
          {/* ---- Місяць ---- */}
          <div className="flex items-center justify-between">
            <button
              type="button"
              onClick={() => shiftMonth(-1)}
              className="btn btn-ghost btn-sm px-2"
              aria-label="Попередній місяць"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <span className="text-body font-medium text-text">
              {MONTHS[month.getMonth()]} {month.getFullYear()}
            </span>
            <button
              type="button"
              onClick={() => shiftMonth(1)}
              className="btn btn-ghost btn-sm px-2"
              aria-label="Наступний місяць"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>

          {/* ---- Сітка днів ---- */}
          <div className="mt-3 grid grid-cols-7 gap-0.5">
            {WEEKDAYS.map((label) => (
              <div key={label} className="pb-1 text-center text-eyebrow font-semibold text-text-faint">
                {label}
              </div>
            ))}

            {days.map((day) => {
              const key = toDateKey(day);
              const outside = day.getMonth() !== month.getMonth();
              const selected = key === selectedKey;
              const disabled = isDisabled(day);

              return (
                <button
                  key={key}
                  type="button"
                  disabled={disabled}
                  onClick={() => pickDate(day)}
                  aria-current={selected ? "date" : undefined}
                  className={`tabular h-8 rounded-md font-mono text-micro transition ${
                    selected
                      ? "bg-ember font-medium text-ink-950"
                      : disabled
                        ? "cursor-not-allowed text-text-faint/40"
                        : outside
                          ? "text-text-faint hover:bg-ink-700"
                          : "text-text-muted hover:bg-ink-700 hover:text-text"
                  } ${!selected && key === todayKey ? "ring-1 ring-inset ring-line" : ""}`}
                >
                  {day.getDate()}
                </button>
              );
            })}
          </div>

          {/* ---- Час ---- */}
          {withTime && (
            <div className="mt-4 border-t border-line-soft pt-3">
              <div className="mb-2 flex items-center gap-1.5 text-eyebrow font-semibold uppercase text-text-faint">
                <Clock className="h-3.5 w-3.5" />
                Час
              </div>

              <div className="flex items-start gap-3">
                <div className="min-w-0 flex-1">
                  <div className="mb-1 text-micro text-text-faint">Година</div>
                  <div className="grid max-h-28 grid-cols-6 gap-0.5 overflow-y-auto pr-1">
                    {HOURS.map((candidate) => (
                      <button
                        key={candidate}
                        type="button"
                        onClick={() => pickTime(candidate, minute)}
                        className={`tabular h-7 rounded-md font-mono text-micro transition ${
                          candidate === hour
                            ? "bg-ember font-medium text-ink-950"
                            : "text-text-muted hover:bg-ink-700 hover:text-text"
                        }`}
                      >
                        {pad(candidate)}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="shrink-0">
                  <div className="mb-1 text-micro text-text-faint">Хвилини</div>
                  <div className="flex gap-0.5">
                    {MINUTES.map((candidate) => (
                      <button
                        key={candidate}
                        type="button"
                        onClick={() => pickTime(hour, candidate)}
                        className={`tabular h-7 w-9 rounded-md font-mono text-micro transition ${
                          candidate === minute
                            ? "bg-ember font-medium text-ink-950"
                            : "text-text-muted hover:bg-ink-700 hover:text-text"
                        }`}
                      >
                        {pad(candidate)}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          )}

          <div className="mt-3 flex items-center justify-between border-t border-line-soft pt-3">
            <span className="tabular font-mono text-micro text-text-faint">{summary || "не обрано"}</span>
            <button type="button" onClick={() => setOpen(false)} className="btn btn-secondary btn-sm">
              Готово
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
