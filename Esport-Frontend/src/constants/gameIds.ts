/**
 * Дзеркало Esport-Backend/Common/GameIdFormats.cs.
 *
 * Тут — підписи, приклади й ті самі формати; там — перевірка, яка вирішує.
 * Той самий поділ, що у games.ts та countries.ts: розійдуться — форма
 * надішле значення, яке валідатор відхилить.
 *
 * Battle.net у переліку є, хоча жодна з чотирьох дисциплін
 * (Esport-Backend/Common/Games.cs) через нього не грається — його попросили
 * окремо, і коштує він одну колонку.
 */

/** Ім'я до решітки — 3..16 символів, тег — 2..5 букв або цифр. */
export const RIOT_ID_PATTERN = /^[^#\s][^#]{1,14}[^#\s]#[A-Za-z0-9]{2,5}$/;

/** 17 цифр від бази 76561197960265728. */
export const STEAM_ID64_PATTERN = /^7656119\d{10}$/;

export const BATTLE_TAG_PATTERN = /^[A-Za-z][A-Za-z0-9]{2,11}#\d{4,5}$/;

export type GameIdField = {
  /** Ключ у DTO — має збігатися з іменем властивості на бекенді. */
  name: "riotId" | "steamId64" | "battleTag";
  label: string;
  /** Дисципліни, у яких цей тег справді щось означає. */
  hint: string;
  placeholder: string;
  pattern: RegExp;
  error: string;
};

export const GAME_ID_FIELDS: GameIdField[] = [
  {
    name: "riotId",
    label: "Riot ID",
    hint: "Valorant, League of Legends",
    placeholder: "Shroud#EUW",
    pattern: RIOT_ID_PATTERN,
    error: "Riot ID має вигляд «Ім'я#TAG», наприклад Shroud#EUW"
  },
  {
    name: "steamId64",
    label: "SteamID64",
    hint: "CS2, Dota 2",
    placeholder: "76561197960265728",
    pattern: STEAM_ID64_PATTERN,
    error: "SteamID64 — це 17 цифр, що починаються з 7656119"
  },
  {
    name: "battleTag",
    label: "BattleTag",
    hint: "Battle.net",
    placeholder: "Player#1234",
    pattern: BATTLE_TAG_PATTERN,
    error: "BattleTag має вигляд «Ім'я#1234»"
  }
];

/**
 * Порожнє поле — це «не вказав», а не помилка, тож перевіряємо лише
 * заповнене. Порожнє віддаємо як undefined: інакше в колонку ляже
 * порожній рядок замість null.
 */
export const isValidGameId = (field: GameIdField, value: string | undefined) =>
  !value || value.trim() === "" || field.pattern.test(value.trim());

export const emptyToUndefined = (value: string | undefined) =>
  value && value.trim() !== "" ? value.trim() : undefined;
