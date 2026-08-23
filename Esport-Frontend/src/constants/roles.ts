/**
 * Підписи ролей українською. Значення мусять збігатися з Common/UserRoles.cs —
 * ключем тут є те саме, що приходить у User.Role.
 */
export const ROLE_LABELS: Record<string, string> = {
  Guest: "Гість",
  Player: "Гравець",
  Organizer: "Організатор",
  Admin: "Адміністратор"
};

export const roleLabel = (role: string) => ROLE_LABELS[role] ?? role;
