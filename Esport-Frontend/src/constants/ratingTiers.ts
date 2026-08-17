/**
 * Дзеркало Esport-Backend/Common/RatingTiers.cs.
 *
 * Ключі приходять із сервера, підписи живуть тут — так само, як у games.ts.
 * Межі рейтингу навмисно не дублюються: лігу визначає сервер одним
 * калькулятором, інакше два джерела правди неминуче розійшлися б.
 */
export const RATING_TIERS = ["Bronze", "Silver", "Gold", "Platinum", "Elite"] as const;

export type RatingTier = (typeof RATING_TIERS)[number];

export const TIER_LABELS: Record<RatingTier, string> = {
  Bronze: "Бронза",
  Silver: "Срібло",
  Gold: "Золото",
  Platinum: "Платина",
  Elite: "Еліта"
};

/**
 * Ліга — це шкала, а не статус, тож вона не бере на себе ані ember, ані win:
 * акцент зарезервовано для головних дій і живого стану. Замість кольору
 * працює насиченість, і лише вершина шкали дістає теплий відтінок.
 */
export const TIER_CLASSES: Record<RatingTier, string> = {
  Bronze: "border-line bg-ink-800 text-text-faint",
  Silver: "border-line bg-ink-800 text-text-muted",
  Gold: "border-line bg-ink-700 text-text",
  Platinum: "border-text-faint/40 bg-ink-700 text-text",
  Elite: "border-ember/40 bg-ember/10 text-ember-soft"
};

export const tierLabel = (tier: string | null | undefined): string =>
  (tier && (TIER_LABELS as Record<string, string>)[tier]) || tier || "";

export const tierClass = (tier: string | null | undefined): string =>
  (tier && (TIER_CLASSES as Record<string, string>)[tier]) || "border-line bg-ink-800 text-text-muted";
