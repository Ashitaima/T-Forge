import { useState } from "react";
import { ChevronDown, ChevronsUpDown, ChevronUp } from "lucide-react";
import type { SortDirection } from "../../constants/sortKeys";

type Props = {
  label: string;
  sortKey: string;
  activeKey: string;
  direction: SortDirection;
  onSort: (key: string) => void;
  align?: "left" | "right";
};

/**
 * Заголовок колонки з сортуванням.
 *
 * Сортує сервер, тож порядок діє на весь набір даних, а не лише на видиму
 * сторінку — інакше на списку з пагінацією сортування було б оманливим.
 */
export const SortableTh = ({
  label,
  sortKey,
  activeKey,
  direction,
  onSort,
  align = "left"
}: Props) => {
  const isActive = activeKey === sortKey;
  const Icon = !isActive ? ChevronsUpDown : direction === "asc" ? ChevronUp : ChevronDown;

  return (
    <th className={align === "right" ? "text-right" : undefined}>
      <button
        type="button"
        onClick={() => onSort(sortKey)}
        aria-sort={isActive ? (direction === "asc" ? "ascending" : "descending") : "none"}
        className={`inline-flex items-center gap-1 transition hover:text-text ${
          isActive ? "text-text" : ""
        } ${align === "right" ? "flex-row-reverse" : ""}`}
      >
        {label}
        <Icon className={`h-3 w-3 ${isActive ? "text-ember" : "text-text-faint"}`} />
      </button>
    </th>
  );
};

/**
 * Стан сортування таблиці. Повторний клік по активній колонці перемикає
 * напрям; клік по іншій починає з висхідного порядку.
 */
export const useSortState = (defaultKey: string, defaultDirection: SortDirection = "desc") => {
  const [sortBy, setSortBy] = useState(defaultKey);
  const [sortDirection, setSortDirection] = useState<SortDirection>(defaultDirection);

  const onSort = (key: string) => {
    if (key === sortBy) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
      return;
    }

    setSortBy(key);
    setSortDirection("asc");
  };

  return { sortBy, sortDirection, onSort };
};
