import { useState } from "react";
import { Link } from "react-router-dom";
import { Bell, X } from "lucide-react";
import { notificationsApi } from "../../api/notificationsApi";
import { EmptyState } from "../ui/Primitives";
import { notificationIcon } from "../../constants/notificationKinds";
import { useUnreadCount } from "../../hooks/useUnreadCount";
import type { NotificationDto } from "../../types";

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("uk-UA", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit"
  });

/**
 * Дзвінок сповіщень.
 *
 * Список тягнемо лише коли відкрили — на таймері крутиться сам лічильник.
 * Дії тут навмисно немає: відповідати на виклик чи заявку треба там, де це
 * робилося завжди, тож картка веде на сторінку команди чи турніру. Одна дія —
 * одне місце.
 */
export const NotificationBell = () => {
  const { count, refresh } = useUnreadCount();
  const [items, setItems] = useState<NotificationDto[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);

  const openBell = async () => {
    setOpen(true);
    setLoading(true);
    try {
      setItems(await notificationsApi.list());
      await notificationsApi.markSeen();
      await refresh();
    } catch {
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button
        type="button"
        onClick={openBell}
        className={`flex w-full items-center gap-2 rounded-lg border px-3 py-2 text-micro text-text transition ${
          count > 0
            ? "border-ember/40 bg-ember/10 hover:bg-ember/15"
            : "border-line bg-ink-900 hover:bg-ink-800"
        }`}
      >
        <Bell className={`h-4 w-4 ${count > 0 ? "text-ember" : "text-text-muted"}`} />
        <span className="flex-1 text-left">Сповіщення</span>
        {count > 0 && <span className="tabular font-mono text-ember">{count}</span>}
      </button>

      {open && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-ink-950/70 p-4"
          role="dialog"
          aria-modal="true"
          aria-label="Сповіщення"
        >
          <div className="panel max-h-[80vh] w-full max-w-lg overflow-y-auto">
            <div className="panel-header">
              <h2 className="section-title">Сповіщення</h2>
              <button
                type="button"
                onClick={() => setOpen(false)}
                className="btn btn-ghost btn-sm px-2"
                aria-label="Закрити"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="panel-body space-y-3">
              {loading && <p className="muted text-micro">Завантаження...</p>}

              {!loading && items.length === 0 && (
                <EmptyState
                  title="У вас поки немає сповіщень"
                  hint="Тут з'являться заявки до команди, виклики на матч і запрошення на турніри."
                />
              )}

              {items.map((item, index) => {
                const Icon = notificationIcon(item.kind);
                return (
                  <Link
                    key={`${item.kind}-${item.createdAt}-${index}`}
                    to={item.link}
                    onClick={() => setOpen(false)}
                    className="surface-raised flex gap-3 px-4 py-3.5 transition hover:bg-ink-800"
                  >
                    <Icon
                      className={`mt-0.5 h-4 w-4 shrink-0 ${
                        item.isActionable ? "text-ember" : "text-text-muted"
                      }`}
                    />
                    {/* min-w-0 — інакше довга назва команди розсуває картку */}
                    <div className="min-w-0 flex-1">
                      <div className="text-body text-text">{item.title}</div>
                      {item.body && <div className="muted mt-1 text-micro">{item.body}</div>}
                      <div className="muted mt-1 flex flex-wrap items-center gap-2 text-micro">
                        <span className="tabular font-mono">{formatDateTime(item.createdAt)}</span>
                        {item.isActionable && <span className="pill">Чекає відповіді</span>}
                      </div>
                    </div>
                  </Link>
                );
              })}
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default NotificationBell;
