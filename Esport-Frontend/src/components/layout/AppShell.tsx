import { useEffect, useMemo, useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { Anvil, CalendarClock, LayoutGrid, ListOrdered, LogOut, Shield, Swords, UserRound, UsersRound } from "lucide-react";
import { useAuthStore } from "../../store/authStore";

const navItems = [
  { to: "/", label: "Огляд", icon: LayoutGrid, end: true },
  { to: "/tournaments", label: "Турніри", icon: Swords },
  { to: "/teams", label: "Команди", icon: UsersRound },
  { to: "/players", label: "Гравці", icon: UserRound },
  { to: "/matches", label: "Матчі", icon: CalendarClock },
  { to: "/standings", label: "Таблиця", icon: ListOrdered },
  { to: "/users", label: "Користувачі", icon: Shield, adminOnly: true }
];

const roleLabels: Record<string, string> = {
  Guest: "Гість",
  User: "Користувач",
  Player: "Гравець",
  Organizer: "Організатор",
  Admin: "Адміністратор"
};

export const AppShell = () => {
  const { user, logout, hydrate } = useAuthStore();
  const [previewRole, setPreviewRole] = useState<string>("");

  const effectiveRole = useMemo(() => previewRole || user?.role || "Guest", [previewRole, user?.role]);

  useEffect(() => {
    hydrate();
  }, [hydrate]);

  const visibleItems = navItems.filter((item) => !item.adminOnly || effectiveRole === "Admin");
  const initials = user ? `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.trim() || user.username[0] : "?";

  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[15rem_1fr]">
      <aside className="flex flex-col border-b border-line bg-ink-950/80 lg:sticky lg:top-0 lg:h-screen lg:border-b-0 lg:border-r">
        <div className="flex items-center gap-2.5 px-5 py-5">
          {/* Клеймо: розпечений знак кузні */}
          <span className="flex h-8 w-8 items-center justify-center rounded-md bg-ember/15 text-ember">
            <Anvil className="h-4 w-4" />
          </span>
          <span className="font-display text-[1.0625rem] font-extrabold tracking-tight text-text">
            T&#8209;Forge
          </span>
        </div>

        <nav className="flex gap-1 overflow-x-auto px-3 pb-3 lg:flex-col lg:overflow-visible lg:pb-0">
          {visibleItems.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `group relative flex items-center gap-2.5 rounded-lg px-3 py-2 text-body transition duration-150 ${
                  isActive ? "bg-ink-800 text-text" : "text-text-muted hover:bg-ink-900 hover:text-text"
                }`
              }
            >
              {({ isActive }) => (
                <>
                  {/* Активний розділ позначено розжареною засічкою */}
                  <span
                    className={`absolute left-0 top-1/2 h-5 w-0.5 -translate-y-1/2 rounded-r bg-ember transition-opacity ${
                      isActive ? "opacity-100" : "opacity-0"
                    }`}
                  />
                  <Icon className={`h-4 w-4 ${isActive ? "text-ember" : ""}`} />
                  {label}
                </>
              )}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto hidden space-y-3 p-3 lg:block">
          {/* Інструмент розробки — унизу й підписаний, щоб не читався як частина продукту */}
          <div className="surface px-3 py-3">
            <label className="eyebrow mb-2 block" htmlFor="role-preview">
              Режим розробника
            </label>
            <select
              id="role-preview"
              value={previewRole}
              onChange={(event) => setPreviewRole(event.target.value)}
              className="input text-micro"
              style={{ height: "2rem" }}
            >
              <option value="">Моя роль</option>
              {Object.entries(roleLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  Показати як: {label}
                </option>
              ))}
            </select>
          </div>

          <div className="surface px-3 py-3">
            {user ? (
              <div className="flex items-center gap-3">
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-ink-700 text-micro font-semibold uppercase text-text">
                  {initials}
                </span>
                <div className="min-w-0 flex-1">
                  <div className="truncate text-micro font-medium text-text">
                    {user.firstName} {user.lastName}
                  </div>
                  <div className="truncate text-micro text-text-faint">
                    {roleLabels[effectiveRole] ?? effectiveRole}
                  </div>
                </div>
                <button onClick={logout} className="btn btn-ghost btn-sm px-2" title="Вийти" aria-label="Вийти">
                  <LogOut className="h-4 w-4" />
                </button>
              </div>
            ) : (
              <NavLink to="/login" className="btn btn-secondary btn-sm w-full">
                Увійти
              </NavLink>
            )}
          </div>
        </div>
      </aside>

      <main className="min-w-0 px-5 py-7 lg:px-10 lg:py-9">
        <div className="mx-auto max-w-6xl space-y-7">
          <Outlet />
        </div>
      </main>
    </div>
  );
};
