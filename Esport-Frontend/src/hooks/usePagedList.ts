import { useCallback, useEffect, useState } from "react";
import type { PagedResponse } from "../types";

/**
 * Стан пагінованого списку: сторінка, підсумки та завантаження.
 *
 * `resetKey` має містити всі значення, які замикає в собі `fetchPage`
 * (пошуковий рядок, id команди тощо). Щойно ключ змінюється, список
 * повертається на першу сторінку й перезавантажується — саме тому
 * `fetchPage` не потрібен у залежностях ефекту.
 */
export function usePagedList<T>(
  fetchPage: (page: number, pageSize: number) => Promise<PagedResponse<T>>,
  resetKey: string,
  pageSize = 20
) {
  const [items, setItems] = useState<T[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [reloadToken, setReloadToken] = useState(0);
  const [appliedKey, setAppliedKey] = useState(resetKey);

  /** Перечитати поточну сторінку — наприклад після видалення запису. */
  const reload = useCallback(() => setReloadToken((token) => token + 1), []);

  // Коли фільтр змінюється, повернути на першу сторінку та застосувати новий ключ
  useEffect(() => {
    if (resetKey !== appliedKey) {
      setPage(1);
      setAppliedKey(resetKey);
    }
  }, [resetKey, appliedKey]);

  useEffect(() => {
    let isActive = true;
    setLoading(true);

    fetchPage(page, pageSize)
      .then((response) => {
        if (!isActive) {
          return;
        }

        setItems(response.data);
        setTotalCount(response.totalCount);
        setTotalPages(response.totalPages);

        // Якщо записів поменшало, не залишаємо користувача на неіснуючій сторінці
        if (response.totalPages === 0) {
          setPage(1);
        } else if (page > response.totalPages) {
          setPage(response.totalPages);
        }
      })
      .catch(() => {
        if (isActive) {
          setItems([]);
          setTotalCount(0);
          setTotalPages(0);
        }
      })
      .finally(() => {
        if (isActive) {
          setLoading(false);
        }
      });

    return () => {
      isActive = false;
    };
    // fetchPage навмисно не в залежностях — його зміни описує resetKey
  }, [appliedKey, page, pageSize, reloadToken]);

  return { items, page, setPage, totalCount, totalPages, loading, reload };
}
