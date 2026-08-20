/**
 * Дзеркало Esport-Backend/Common/Regions.cs.
 *
 * Значення (ключі) — це те, що зберігає база й перевіряє валідатор.
 * Підписи — те, що бачить користувач. Той самий поділ, що у games.ts та
 * countries.ts: змінюючи список тут, змініть його і на бекенді.
 */
export const REGIONS = [
  "Europe",
  "North America",
  "South America",
  "CIS",
  "Asia",
  "Oceania",
  "Middle East",
  "Africa"
] as const;

export type Region = (typeof REGIONS)[number];

export const REGION_LABELS: Record<Region, string> = {
  Europe: "Європа",
  "North America": "Північна Америка",
  "South America": "Південна Америка",
  CIS: "СНД",
  Asia: "Азія",
  Oceania: "Океанія",
  "Middle East": "Близький Схід",
  Africa: "Африка"
};

/** Підпис для значення з бекенда, яке може бути й невідомим (старі дані). */
export const regionLabel = (region: string): string =>
  (REGION_LABELS as Record<string, string>)[region] ?? region;
