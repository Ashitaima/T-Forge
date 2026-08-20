/**
 * Дзеркало Esport-Backend/Common/StatusConstants.cs → MatchTypes.
 *
 * Це не «вид матчу» в сенсі турнірний/практичний, а **стадія турнірної
 * сітки**: груповий етап, чвертьфінал, фінал і так далі. Тому поле має сенс
 * лише всередині турніру — практичний матч і дуель стадії не мають, і сервер
 * ставить їм GroupStage як нейтральне значення.
 *
 * Значення не декоративне: EloCalculator дає фіналу й матчу за третє місце
 * більший коефіцієнт K, тож помилкова стадія змінює рейтинг.
 */
export const MATCH_TYPES = [
  "GroupStage",
  "PlayIn",
  "RoundOf32",
  "RoundOf16",
  "QuarterFinal",
  "SemiFinal",
  "Final",
  "ThirdPlace"
] as const;

export const MATCH_TYPE_LABELS: Record<string, string> = {
  GroupStage: "Груповий етап",
  PlayIn: "Кваліфікація",
  RoundOf32: "1/16 фіналу",
  RoundOf16: "1/8 фіналу",
  QuarterFinal: "Чвертьфінал",
  SemiFinal: "Півфінал",
  Final: "Фінал",
  ThirdPlace: "Матч за третє місце"
};

export const matchTypeLabel = (value: string) => MATCH_TYPE_LABELS[value] ?? value;
