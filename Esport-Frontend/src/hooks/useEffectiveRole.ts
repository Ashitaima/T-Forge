import { useAuthStore } from "../store/authStore";

/**
 * Єдине джерело правди для рольових перевірок в інтерфейсі.
 *
 * Режим розробника дозволяє адміністраторові подивитися на застосунок очима
 * іншої ролі. Підміна діє тільки на клієнті: запити до API й далі йдуть із
 * справжнім токеном. Тому всі перевірки виду «показати кнопку / пустити на
 * маршрут» мають читати саме цей хук, а не user.role напряму — інакше частина
 * інтерфейсу залишиться в реальній ролі й режим працюватиме наполовину.
 */
export const useEffectiveRole = (): string => {
  const user = useAuthStore((state) => state.user);
  const previewRole = useAuthStore((state) => state.previewRole);

  // Підміняти роль може лише справжній адміністратор.
  if (user?.role === "Admin" && previewRole) {
    return previewRole;
  }

  return user?.role ?? "Guest";
};

/** Чи відповідає поточна (можливо, підмінена) роль хоча б одній із перелічених. */
export const useIsRole = (...roles: string[]): boolean => {
  const effectiveRole = useEffectiveRole();
  return roles.includes(effectiveRole);
};

/**
 * Чи є поточний користувач капітаном цієї команди — з урахуванням режиму
 * розробника.
 *
 * Капітанство не є роллю: у моделі це Team.CaptainId, тобто звʼязок із
 * конкретною командою. Тому рольовий перемикач його не виражає, і адміністратор
 * обирає команду окремо. Підміна діє лише в інтерфейсі: запити й далі йдуть від
 * справжнього адміністратора, у якого права на ці дії є й без неї.
 *
 * Це навмисний виняток із правила «власника звіряй за справжнім id»: тут
 * підміна нічого не відкриває, бо доступна лише адміністраторові й нічого не
 * додає до його прав на сервері.
 */
export const useIsCaptainOf = (
  teamId: number | null | undefined,
  teamCaptainId: number | null | undefined
): boolean => {
  const user = useAuthStore((state) => state.user);
  const previewCaptainTeamId = useAuthStore((state) => state.previewCaptainTeamId);

  // Під час підміни капітанською вважається рівно одна команда — обрана.
  if (user?.role === "Admin" && previewCaptainTeamId > 0) {
    return teamId === previewCaptainTeamId;
  }

  return Boolean(user && teamCaptainId != null && user.id === teamCaptainId);
};

/** Чи активний режим розробника — потрібно для банера. */
export const useIsPreviewing = (): boolean => {
  const user = useAuthStore((state) => state.user);
  const previewRole = useAuthStore((state) => state.previewRole);
  const previewCaptainTeamId = useAuthStore((state) => state.previewCaptainTeamId);
  return user?.role === "Admin" && (Boolean(previewRole) || previewCaptainTeamId > 0);
};
