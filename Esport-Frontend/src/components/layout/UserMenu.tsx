import { ChevronDown, LogOut, Settings, UserRound } from "lucide-react";
import { Link } from "react-router-dom";
import { useAuthStore } from "../../store/authStore";
import { useEffectiveRole } from "../../hooks/useEffectiveRole";
import { useMyPlayer } from "../../hooks/useMyPlayer";
import { roleLabel } from "../../constants/roles";
import { Avatar } from "../ui/Avatar";
import { Menu, MenuSeparator, menuItemClass } from "../ui/Menu";
import type { UserInfo } from "../../store/authStore";

/**
 * Меню облікового запису.
 *
 * Пункт «Мій профіль» веде на публічну сторінку гравця — ту, яку бачать інші, —
 * а «Налаштування» на /profile, де її редагують. Це різні речі, і раніше з
 * оболонки не було виходу на першу з них узагалі.
 *
 * Роль читаємо через useEffectiveRole, як і решта інтерфейсу: у режимі
 * розробника меню має показувати підмінену роль, інакше воно єдине лишається
 * зі справжньою і суперечить банеру нагорі.
 */
export const UserMenu = ({ user }: { user: UserInfo }) => {
  const logout = useAuthStore((state) => state.logout);
  const effectiveRole = useEffectiveRole();
  const player = useMyPlayer();

  const fullName = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim();
  // Нікнейм — те, під чим користувача знають у системі; ім'я лишається
  // відкатом для організатора й адміністратора, у яких профілю гравця немає.
  const displayName = player?.nickname || fullName || user.username;
  const initials = `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.trim() || user.username[0];

  return (
    <Menu
      label="Меню облікового запису"
      align="end"
      className="w-56"
      triggerClassName="flex items-center gap-2 rounded-lg border border-line bg-ink-900 py-1 pl-1 pr-2 text-micro text-text transition hover:bg-ink-800"
      trigger={
        <>
          <Avatar url={user.avatarUrl} fallback={initials} size="sm" />
          {/* На вузькому екрані лишається сам аватар: інакше рядок із логотипом
              і дзвінком переноситься. */}
          <span className="hidden max-w-[8rem] truncate sm:block">{displayName}</span>
          <ChevronDown className="h-3.5 w-3.5 text-text-faint" />
        </>
      }
    >
      {(close) => (
        <>
          <div className="flex items-center gap-3 px-3 py-3">
            <Avatar url={user.avatarUrl} fallback={initials} size="md" />
            <div className="min-w-0">
              <div className="truncate text-micro font-medium text-text">{displayName}</div>
              <div className="truncate text-micro text-text-faint">@{user.username}</div>
              <span className="pill pill-neutral mt-1.5">{roleLabel(effectiveRole)}</span>
            </div>
          </div>

          <MenuSeparator />

          {/* Публічної сторінки немає в того, хто не гравець, — і пункта теж. */}
          {player && (
            <Link to={`/players/${player.playerId}`} onClick={close} role="menuitem" className={menuItemClass}>
              <UserRound className="h-4 w-4" />
              Мій профіль
            </Link>
          )}

          <Link to="/profile" onClick={close} role="menuitem" className={menuItemClass}>
            <Settings className="h-4 w-4" />
            Налаштування
          </Link>

          <MenuSeparator />

          <button type="button" role="menuitem" onClick={logout} className={menuItemClass}>
            <LogOut className="h-4 w-4" />
            Вийти
          </button>
        </>
      )}
    </Menu>
  );
};

export default UserMenu;
