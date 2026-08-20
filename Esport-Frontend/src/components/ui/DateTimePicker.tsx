import { useEffect, useMemo, useRef, useState } from "react";
import { CalendarDays, ChevronLeft, ChevronRight, Clock } from "lucide-react";

/**
 * Вибір дати й часу.
 *
 * Нативний input[type=datetime-local] показує маску «дд.мм.рррр --:--», у якій
 * доводиться потрапляти курсором в окремі сегменти й друкувати цифри наосліп.
 * Тут дата й час — два окремі поля, кожне зі своєю панеллю: календар для дня,
 * циферблат для часу. Обидва поля лишаються текстовими, тож будь-яку хвилину
 * можна просто набрати; циферблат — для випадків, коли зручніше показати.
 *
 * Значення назовні — той самий рядок, що й у нативних полів: «YYYY-MM-DDTHH:mm»
 * у режимі datetime і «YYYY-MM-DD» у режимі date. Форми, що на них спираються,
 * не змінюються — назовні це як був один контрол, так і лишився.
 */

const WEEKDAYS = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд"];

const MONTHS = [
  "Січень", "Лютий", "Березень", "Квітень", "Травень", "Червень",
  "Липень", "Серпень", "Вересень", "Жовтень", "Листопад", "Грудень"
];

const TIME_PATTERN = /^(\d{1,2}):(\d{2})$/;

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

const timeLabel = (hour: number | null, minute: number | null) =>
  hour === null ? "" : `${pad(hour)}:${pad(minute ?? 0)}`;

/* ------------------------------------------------------------------ */
/* Циферблат                                                          */
/* ------------------------------------------------------------------ */

/**
 * Доба тут двадцятичотиригодинна, тож циферблат — теж: 24 поділки по 15°, а не
 * дванадцять із AM/PM, якого в українському інтерфейсі немає. Через це стрілка
 * о 18:00 дивиться ліворуч, а не як на настінному годиннику — зате жодної
 * здогадки, ранок це чи вечір. Хвилини — звичні 60 поділок по 6°.
 */
const DIAL = {
  hour: { step: 15, range: 24, marks: [0, 3, 6, 9, 12, 15, 18, 21], hand: 26 },
  minute: { step: 6, range: 60, marks: [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55], hand: 18 }
} as const;

type DialTarget = keyof typeof DIAL;

/** Точка на колі: 0° — вгору, далі за годинниковою стрілкою. */
const polar = (angle: number, radius: number) => {
  const radians = ((angle - 90) * Math.PI) / 180;
  return { x: 50 + radius * Math.cos(radians), y: 50 + radius * Math.sin(radians) };
};

type DialProps = {
  hour: number;
  minute: number;
  target: DialTarget;
  onPick: (hour: number, minute: number) => void;
};

const ClockDial = ({ hour, minute, target, onPick }: DialProps) => {
  const ref = useRef<SVGSVGElement>(null);
  const [dragging, setDragging] = useState(false);

  const { step, range, marks } = DIAL[target];
  const current = target === "hour" ? hour : minute;

  const commit = (next: number) =>
    onPick(target === "hour" ? next : hour, target === "hour" ? minute : next);

  /** Кут від центру до курсора → найближча поділка. */
  const applyPointer = (clientX: number, clientY: number) => {
    const rect = ref.current?.getBoundingClientRect();
    if (!rect) {
      return;
    }

    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;
    const degrees =
      ((Math.atan2(clientX - centerX, centerY - clientY) * 180) / Math.PI + 360) % 360;

    commit(Math.round(degrees / step) % range);
  };

  return (
    <svg
      ref={ref}
      viewBox="0 0 100 100"
      className="h-40 w-40 cursor-pointer touch-none rounded-full outline-none ring-ember/50 focus-visible:ring-2"
      role="slider"
      aria-label={target === "hour" ? "Година" : "Хвилини"}
      aria-valuemin={0}
      aria-valuemax={range - 1}
      aria-valuenow={current}
      aria-valuetext={pad(current)}
      tabIndex={0}
      onPointerDown={(event) => {
        event.currentTarget.setPointerCapture(event.pointerId);
        setDragging(true);
        applyPointer(event.clientX, event.clientY);
      }}
      onPointerMove={(event) => {
        if (dragging) {
          applyPointer(event.clientX, event.clientY);
        }
      }}
      onPointerUp={(event) => {
        event.currentTarget.releasePointerCapture(event.pointerId);
        setDragging(false);
      }}
      onPointerCancel={() => setDragging(false)}
      onKeyDown={(event) => {
        const delta =
          event.key === "ArrowUp" || event.key === "ArrowRight"
            ? 1
            : event.key === "ArrowDown" || event.key === "ArrowLeft"
              ? -1
              : 0;

        if (delta !== 0) {
          event.preventDefault();
          commit((current + delta + range) % range);
        }
      }}
    >
      <circle cx="50" cy="50" r="46" className="fill-ink-950 stroke-line" strokeWidth="1.5" />

      {/* Поділки: великі там, де стоїть підпис */}
      <g className="text-line" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round">
        {Array.from({ length: range }, (_, index) => {
          const major = (marks as readonly number[]).includes(index);
          const outer = polar(index * step, 44);
          const inner = polar(index * step, major ? 39.5 : 42);
          return (
            <line
              key={index}
              x1={inner.x}
              y1={inner.y}
              x2={outer.x}
              y2={outer.y}
              opacity={major ? 1 : 0.45}
            />
          );
        })}
      </g>

      {/* Підписи — тих одиниць, які зараз крутять */}
      <g className="fill-text-faint font-mono" fontSize="7.5" textAnchor="middle">
        {marks.map((mark) => {
          const point = polar(mark * step, 31);
          return (
            <text key={mark} x={point.x} y={point.y} dy="2.6">
              {pad(mark)}
            </text>
          );
        })}
      </g>

      {/* Обидві стрілки видно завжди — час читається цілком, а не половинами.
          Під час перетягування перехід вимкнено: інакше стрілка йшла б за
          курсором із запізненням і на швидкому русі відставала. */}
      {(["hour", "minute"] as const).map((which) => {
        const active = which === target;
        const angle = which === "hour" ? hour * DIAL.hour.step : minute * DIAL.minute.step;
        const length = DIAL[which].hand;

        return (
          <g
            key={which}
            className={active ? "text-ember" : "text-text-faint"}
            style={{
              transform: `rotate(${angle}deg)`,
              transformOrigin: "50px 50px",
              transition: dragging ? "none" : "transform 200ms ease-out"
            }}
          >
            <line
              x1="50"
              y1="50"
              x2="50"
              y2={length}
              stroke="currentColor"
              strokeWidth={which === "hour" ? 4 : 2.5}
              strokeLinecap="round"
            />
            {active && <circle cx="50" cy={length} r="5.5" fill="currentColor" />}
          </g>
        );
      })}

      <circle cx="50" cy="50" r="3" className="fill-ember" />
    </svg>
  );
};

/* ------------------------------------------------------------------ */

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

  const [open, setOpen] = useState<"none" | "date" | "time">("none");
  const [dialTarget, setDialTarget] = useState<DialTarget>("hour");
  const [month, setMonth] = useState(() => parsed?.date ?? new Date());
  const containerRef = useRef<HTMLDivElement>(null);

  // Поля тримають власний текст: поки набирають «2026-08-1», значення ще не
  // склалося, і віддавати назовні півдати не можна. Ефекти нижче підтягують
  // текст лише коли value справді змінилося — тобто ззовні або з панелі.
  const [dateText, setDateText] = useState(() => (parsed ? toDateKey(parsed.date) : ""));
  const [timeText, setTimeText] = useState(() =>
    parsed ? timeLabel(parsed.hour, parsed.minute) : ""
  );

  useEffect(() => {
    const next = parseValue(value);
    const target = next ? toDateKey(next.date) : "";
    setDateText((current) => (current === target ? current : target));
  }, [value]);

  useEffect(() => {
    const next = parseValue(value);
    const target = next ? timeLabel(next.hour, next.minute) : "";
    setTimeText((current) => (current === target ? current : target));
  }, [value]);

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
    if (open === "none") {
      return;
    }

    const onPointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen("none");
      }
    };

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen("none");
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
    setOpen("none"); // дата й час тепер окремі поля, тож вибір дня завершено
  };

  // Час без дати нічого не означає — якщо її ще нема, беремо сьогодні.
  const pickTime = (nextHour: number, nextMinute: number) =>
    emit(parsed?.date ?? new Date(), nextHour, nextMinute);

  const onDateText = (text: string) => {
    setDateText(text);

    if (text.trim() === "") {
      onChange(""); // порожнє поле мусить дійти до валідатора саме порожнім
      return;
    }

    const next = parseValue(text);
    if (next) {
      emit(next.date, hour, minute);
    }
  };

  const onTimeText = (text: string) => {
    setTimeText(text);

    const match = TIME_PATTERN.exec(text.trim());
    if (!match) {
      return;
    }

    const nextHour = Number(match[1]);
    const nextMinute = Number(match[2]);
    if (nextHour <= 23 && nextMinute <= 59) {
      pickTime(nextHour, nextMinute);
    }
  };

  /** Пішли з поля — недонабране повертаємо до того, що справді записано. */
  const settleText = () => {
    const next = parseValue(value);
    setDateText(next ? toDateKey(next.date) : "");
    setTimeText(next ? timeLabel(next.hour, next.minute) : "");
  };

  const isDisabled = (day: Date) => Boolean(minDate) && toDateKey(day) < minDate!;

  const shiftMonth = (delta: number) =>
    setMonth(new Date(month.getFullYear(), month.getMonth() + delta, 1));

  const toggle = (panel: "date" | "time") =>
    setOpen((current) => (current === panel ? "none" : panel));

  return (
    <div ref={containerRef} className="flex gap-2">
      {/* ---------------- Дата ---------------- */}
      <div className="relative flex-1">
        <input
          id={id}
          type="text"
          value={dateText}
          onChange={(event) => onDateText(event.target.value)}
          onFocus={() => setOpen("date")}
          onBlur={settleText}
          placeholder="РРРР-ММ-ДД"
          aria-label={withTime && ariaLabel ? `${ariaLabel} — дата` : ariaLabel}
          autoComplete="off"
          className="input pr-10 font-mono"
        />
        <button
          type="button"
          onClick={() => toggle("date")}
          aria-label={open === "date" ? "Сховати календар" : "Показати календар"}
          aria-expanded={open === "date"}
          className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-text-faint transition hover:bg-ink-800 hover:text-text"
        >
          <CalendarDays className="h-4 w-4" />
        </button>

        {open === "date" && (
          <div className="surface-raised absolute left-0 z-30 mt-2 w-[19rem] p-4">
            <div className="flex items-center gap-2 border-b border-line-soft pb-3">
              <CalendarDays className="h-4 w-4 text-ember" />
              <h3 className="section-title text-body">Оберіть дату</h3>
            </div>

            <div className="mt-3 flex items-center justify-between">
              <button
                type="button"
                onClick={() => shiftMonth(-1)}
                className="flex h-7 w-7 items-center justify-center rounded-md border border-line text-text-muted transition hover:border-text-faint hover:bg-ink-800 hover:text-text"
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
                className="flex h-7 w-7 items-center justify-center rounded-md border border-line text-text-muted transition hover:border-text-faint hover:bg-ink-800 hover:text-text"
                aria-label="Наступний місяць"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>

            <div className="mt-3 grid grid-cols-7 gap-y-0.5">
              {WEEKDAYS.map((label) => (
                <div
                  key={label}
                  className="pb-1 text-center text-eyebrow font-semibold text-text-faint"
                >
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
                    /* h-8 w-8 і mx-auto, а не розтягнута клітинка: коло має
                       бути колом, інакше позначка виходить овальною. */
                    className={`tabular mx-auto flex h-8 w-8 items-center justify-center rounded-full font-mono text-micro transition ${
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
          </div>
        )}
      </div>

      {/* ---------------- Час ---------------- */}
      {withTime && (
        <div className="relative w-[7.5rem]">
          <input
            type="text"
            value={timeText}
            onChange={(event) => onTimeText(event.target.value)}
            onFocus={() => setOpen("time")}
            onBlur={settleText}
            placeholder="ГГ:ХХ"
            inputMode="numeric"
            aria-label={ariaLabel ? `${ariaLabel} — час` : "Час"}
            autoComplete="off"
            className="input pr-10 font-mono"
          />
          <button
            type="button"
            onClick={() => toggle("time")}
            aria-label={open === "time" ? "Сховати циферблат" : "Показати циферблат"}
            aria-expanded={open === "time"}
            className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-text-faint transition hover:bg-ink-800 hover:text-text"
          >
            <Clock className="h-4 w-4" />
          </button>

          {open === "time" && (
            <div className="surface-raised absolute right-0 z-30 mt-2 w-[15rem] p-4">
              <div className="flex items-center gap-2 border-b border-line-soft pb-3">
                <Clock className="h-4 w-4 text-ember" />
                <h3 className="section-title text-body">Вкажіть час</h3>
              </div>

              {/* Показник і перемикач заразом: підсвічена половина — та, яку
                  крутить циферблат. Точну хвилину можна набрати в полі. */}
              <div className="mt-3 flex items-center justify-center gap-1">
                <button
                  type="button"
                  onClick={() => setDialTarget("hour")}
                  aria-pressed={dialTarget === "hour"}
                  className={`tabular rounded-lg px-3 py-1 font-mono text-h2 transition ${
                    dialTarget === "hour"
                      ? "bg-ember text-ink-950"
                      : "text-text-muted hover:bg-ink-800 hover:text-text"
                  }`}
                >
                  {pad(hour)}
                </button>
                <span className="tabular font-mono text-h2 text-text-faint">:</span>
                <button
                  type="button"
                  onClick={() => setDialTarget("minute")}
                  aria-pressed={dialTarget === "minute"}
                  className={`tabular rounded-lg px-3 py-1 font-mono text-h2 transition ${
                    dialTarget === "minute"
                      ? "bg-ember text-ink-950"
                      : "text-text-muted hover:bg-ink-800 hover:text-text"
                  }`}
                >
                  {pad(minute)}
                </button>
              </div>

              <p className="mt-1.5 text-center text-micro text-text-faint">
                {dialTarget === "hour" ? "Циферблат крутить години" : "Циферблат крутить хвилини"}
              </p>

              <div className="mt-3 flex justify-center">
                <ClockDial hour={hour} minute={minute} target={dialTarget} onPick={pickTime} />
              </div>

              <button
                type="button"
                onClick={() => setOpen("none")}
                className="btn btn-primary mt-4 w-full"
              >
                Готово
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
