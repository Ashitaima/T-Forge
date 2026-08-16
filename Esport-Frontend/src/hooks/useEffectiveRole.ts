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

/** Чи активний режим розробника — потрібно для банера. */
export const useIsPreviewing = (): boolean => {
  const user = useAuthStore((state) => state.user);
  const previewRole = useAuthStore((state) => state.previewRole);
  return user?.role === "Admin" && Boolean(previewRole);
};
