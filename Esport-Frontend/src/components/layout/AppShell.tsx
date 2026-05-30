import { useEffect, useMemo, useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { CalendarClock, Gamepad2, LayoutGrid, Shield, Swords, UserRound, UsersRound } from "lucide-react";
import { useAuthStore } from "../../store/authStore";

const navItems = [
  { to: "/", label: "Дашборд", icon: LayoutGrid },
  { to: "/tournaments", label: "Турніри", icon: Swords },
  { to: "/teams", label: "Команди", icon: UsersRound },
  { to: "/players", label: "Гравці", icon: UserRound },
  { to: "/matches", label: "Матчі", icon: CalendarClock },
  { to: "/users", label: "Користувачі", icon: Shield, adminOnly: true }
];

export const AppShell = () => {
  const { isAuthenticated, user, logout, hydrate } = useAuthStore();
  const [previewRole, setPreviewRole] = useState<string>("");

  const effectiveRole = useMemo(() => {
    if (previewRole) {
      return previewRole;
    }
    return user?.role ?? "Guest";
  }, [previewRole, user?.role]);

  useEffect(() => {
    hydrate();
  }, [hydrate]);

  const visibleItems = navItems.filter((item) => !item.adminOnly || effectiveRole === "Admin");

  return (
    <div className="min-h-screen grid grid-cols-[240px_1fr] bg-night-900 text-slate-100">
      <aside className="border-r border-white/5 bg-night-800/60 px-5 py-6">
        <div className="flex items-center gap-3 text-lg font-semibold text-neon-cyan">
          <Gamepad2 className="h-5 w-5" />
          T-Forge
        </div>
        <div className="mt-8 rounded-xl border border-white/10 bg-night-700/40 p-3 text-xs">
          <div className="text-slate-400">Превʼю ролі</div>
          <select
            value={previewRole}
            onChange={(event) => setPreviewRole(event.target.value)}
            className="mt-2 w-full rounded-lg border border-white/10 bg-night-800/60 px-2 py-1 text-xs text-slate-200"
          >
            <option value="">Поточна роль</option>
            <option value="Guest">Гість</option>
            <option value="User">Користувач</option>
            <option value="Player">Гравець</option>
            <option value="Organizer">Організатор</option>
            <option value="Admin">Адміністратор</option>
          </select>
        </div>
        <nav className="mt-6 space-y-2">
          {visibleItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-xl px-3 py-2 text-sm transition ${
                  isActive ? "bg-neon-cyan/10 text-neon-cyan shadow-neon" : "text-slate-200/80"
                }`
              }
            >
              <Icon className="h-4 w-4" />
              {label}
            </NavLink>
          ))}
        </nav>
        <div className="mt-10 rounded-xl border border-white/10 bg-night-700/40 p-4 text-xs text-slate-300">
          <div className="flex items-center gap-2 text-neon-green">
            <Shield className="h-4 w-4" />
            {isAuthenticated ? "Підключено" : "Гість"}
          </div>
          <div className="mt-2 text-slate-400">Роль: {effectiveRole}</div>
          {user && (
            <div className="mt-2 text-slate-200">
              {user.firstName} {user.lastName}
              <div className="text-xs text-slate-400">@{user.username}</div>
            </div>
          )}
          <div className="mt-3 flex gap-2">
            {isAuthenticated ? (
              <button
                onClick={logout}
                className="rounded-lg border border-neon-magenta/40 px-3 py-1 text-neon-magenta hover:bg-neon-magenta/10"
              >
                Вийти
              </button>
            ) : (
              <NavLink
                to="/login"
                className="rounded-lg border border-neon-cyan/40 px-3 py-1 text-neon-cyan hover:bg-neon-cyan/10"
              >
                Увійти
              </NavLink>
            )}
          </div>
        </div>
      </aside>
      <main className="px-10 py-8">
        <Outlet />
      </main>
    </div>
  );
};
