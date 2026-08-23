/**
 * Дзеркало Esport-Backend/Common/TimeZones.cs — там значення, тут підписи.
 *
 * Зсув зберігається у хвилинах: Індія — UTC+5:30, Непал — UTC+5:45.
 */
export const UTC_OFFSETS: readonly number[] = [
  -720, -660, -600, -570, -540, -480, -420, -360, -300, -240, -210,
  -180, -120, -60,
  0,
  60, 120, 180, 210, 240, 270, 300, 330, 345, 360, 390, 420, 480,
  540, 570, 600, 630, 660, 720, 765, 780, 840
];

const pad = (value: number) => String(value).padStart(2, "0");

export const offsetLabel = (minutes: number | null | undefined): string => {
  if (minutes === null || minutes === undefined) {
    return "Не вказано";
  }
  const sign = minutes < 0 ? "−" : "+";
  const absolute = Math.abs(minutes);
  return `UTC${sign}${pad(Math.floor(absolute / 60))}:${pad(absolute % 60)}`;
};

/**
 * Наскільки співрозмовник далі за годинником від того, хто дивиться.
 * Порожньо, якщо хтось із двох зсуву не вказав.
 */
export const offsetDistanceLabel = (
  theirs: number | null | undefined,
  mine: number | null | undefined
): string | null => {
  if (theirs === null || theirs === undefined || mine === null || mine === undefined) {
    return null;
  }
  const hours = Math.abs(theirs - mine) / 60;
  if (hours === 0) {
    return "той самий час";
  }
  const rendered = Number.isInteger(hours) ? String(hours) : hours.toFixed(1).replace(".", ",");
  return `різниця ${rendered} год`;
};
