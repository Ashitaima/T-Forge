/**
 * Дзеркало Esport-Backend/Common/TimeControls.cs.
 *
 * Контроль часу шахової партії — і домовленість між гравцями, і ознака
 * моделі прогнозу. Пороги ті самі, що в Lichess.
 */
export const TIME_CONTROLS = ["Bullet", "Blitz", "Rapid", "Classical"] as const;

export type TimeControl = (typeof TIME_CONTROLS)[number];

/** Командні турніри без указаного контролю вважаються рапідом (TimeControls.Default). */
export const DEFAULT_TIME_CONTROL: TimeControl = "Rapid";

export const TIME_CONTROL_LABELS: Record<TimeControl, string> = {
  Bullet: "Кулі (до 3 хв)",
  Blitz: "Бліц (3–8 хв)",
  Rapid: "Рапід (8–25 хв)",
  Classical: "Класика (25+ хв)"
};

export const timeControlLabel = (value: string | null | undefined): string =>
  (TIME_CONTROL_LABELS as Record<string, string>)[value ?? DEFAULT_TIME_CONTROL] ?? value ?? "";
