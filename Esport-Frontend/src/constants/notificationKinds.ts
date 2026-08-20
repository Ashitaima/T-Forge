import { Swords, Trophy, UsersRound } from "lucide-react";
import type { NotificationKind } from "../types";

/**
 * Іконка за видом сповіщення. Самі підписи приходять із сервера вже
 * українською — там вони складаються з назв команд і турнірів, тож
 * тримати їх тут означало б збирати той самий рядок удруге.
 */
export const notificationIcon = (kind: NotificationKind) => {
  if (kind.startsWith("Challenge")) {
    return Swords;
  }
  if (kind.startsWith("Tournament")) {
    return Trophy;
  }
  return UsersRound;
};
