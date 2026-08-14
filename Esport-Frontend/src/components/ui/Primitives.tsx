import { Search } from "lucide-react";
import type { ReactNode } from "react";

export const SearchField = ({
  value,
  onChange,
  placeholder
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
}) => (
  <div className="relative max-w-sm flex-1">
    <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-text-faint" />
    <input
      type="search"
      value={value}
      onChange={(event) => onChange(event.target.value)}
      placeholder={placeholder}
      aria-label={placeholder}
      className="input pl-9"
    />
  </div>
);

/** Заголовок сторінки: надрядок, назва, пояснення та головна дія. */
export const PageHeader = ({
  eyebrow,
  title,
  description,
  action
}: {
  eyebrow?: string;
  title: string;
  description?: string;
  action?: ReactNode;
}) => (
  <header className="flex flex-wrap items-end justify-between gap-4 border-b border-line-soft pb-6">
    <div className="max-w-2xl">
      {eyebrow && <div className="eyebrow mb-2">{eyebrow}</div>}
      <h1 className="page-title">{title}</h1>
      {description && <p className="muted mt-2 text-body">{description}</p>}
    </div>
    {action}
  </header>
);

/** Статус як єдина система: колір означає стан, а не настрій. */
export const StatusPill = ({ status }: { status: string }) => {
  const map: Record<string, { label: string; className: string }> = {
    Registration: { label: "Реєстрація", className: "pill-neutral" },
    Scheduled: { label: "Заплановано", className: "pill-neutral" },
    InProgress: { label: "У процесі", className: "pill-live" },
    Completed: { label: "Завершено", className: "pill-done" },
    Cancelled: { label: "Скасовано", className: "pill-off" },
    Postponed: { label: "Перенесено", className: "pill-off" }
  };

  const entry = map[status] ?? { label: status, className: "pill-neutral" };

  return (
    <span className={`pill ${entry.className}`}>
      <span className={`dot ${status === "InProgress" ? "animate-heat-pulse" : ""}`} />
      {entry.label}
    </span>
  );
};

/** Порожній екран — це запрошення до дії, а не сіре речення. */
export const EmptyState = ({
  title,
  hint,
  action
}: {
  title: string;
  hint?: string;
  action?: ReactNode;
}) => (
  <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-line px-6 py-12 text-center">
    <p className="text-lead font-medium text-text">{title}</p>
    {hint && <p className="muted mt-1.5 max-w-sm text-body">{hint}</p>}
    {action && <div className="mt-5">{action}</div>}
  </div>
);

export const Skeleton = ({ rows = 3 }: { rows?: number }) => (
  <div className="space-y-3" aria-hidden>
    {Array.from({ length: rows }).map((_, index) => (
      <div key={index} className="surface flex items-center gap-4 px-5 py-4">
        <div className="skeleton h-9 w-9 rounded-lg" />
        <div className="flex-1 space-y-2">
          <div className="skeleton h-3 w-1/3" />
          <div className="skeleton h-3 w-1/5" />
        </div>
      </div>
    ))}
  </div>
);

/**
 * Показник: велике число живе у моно-шрифті, щоб читатись як дані,
 * а не як заголовок.
 */
export const StatCard = ({
  label,
  value,
  hint,
  icon,
  accent = false
}: {
  label: string;
  value: string;
  hint?: string;
  icon?: ReactNode;
  accent?: boolean;
}) => (
  <div className="surface-raised px-5 py-4">
    <div className="flex items-center justify-between">
      <span className="eyebrow">{label}</span>
      {icon && <span className="text-text-faint">{icon}</span>}
    </div>
    <div
      className={`tabular mt-3 font-mono text-[1.75rem] font-medium leading-none ${
        accent ? "text-ember" : "text-text"
      }`}
    >
      {value}
    </div>
    {hint && <div className="muted mt-2 text-micro">{hint}</div>}
  </div>
);
