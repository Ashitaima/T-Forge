import { useCallback, useEffect, useState } from "react";
import { notificationsApi } from "../api/notificationsApi";

/** Опитуємо раз на хвилину: сповіщення не женуться за секундами. */
const POLL_MS = 60_000;

/**
 * Лічильник непрочитаних сповіщень.
 *
 * Опитування, а не SignalR: так вимагає Scope.md, і воно переживає
 * перепідключення без окремої логіки. MatchHub показує, як під'єднати
 * push, якщо колись знадобиться, — межа сервісу залишає для цього місце.
 */
export const useUnreadCount = () => {
  const [count, setCount] = useState(0);

  const refresh = useCallback(async () => {
    try {
      setCount(await notificationsApi.unreadCount());
    } catch {
      // Мовчки: збій лічильника не повинен ламати оболонку застосунку.
    }
  }, []);

  useEffect(() => {
    refresh();

    const timer = window.setInterval(refresh, POLL_MS);
    // Повернення на вкладку — найдешевший момент дізнатися новини.
    window.addEventListener("focus", refresh);

    return () => {
      window.clearInterval(timer);
      window.removeEventListener("focus", refresh);
    };
  }, [refresh]);

  return { count, refresh };
};
