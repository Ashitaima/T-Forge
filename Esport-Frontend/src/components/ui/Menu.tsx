import { useEffect, useRef, useState, type ReactNode } from "react";
import { useLocation } from "react-router-dom";

type Props = {
  /** Вміст кнопки, яка відкриває меню. */
  trigger: ReactNode;
  /** Доступна назва кнопки — її читає скрінрідер замість вмісту. */
  label: string;
  /** До якого краю кнопки притискаємо панель. */
  align?: "start" | "end";
  triggerClassName?: string;
  /** Ширина панелі та інші правки саме цього меню. */
  className?: string;
  /** Вміст меню. Аргумент закриває панель — його чіпляють до пунктів-дій. */
  children: (close: () => void) => ReactNode;
};

/**
 * Випадне меню, прив'язане до кнопки.
 *
 * Дзвінок сповіщень свого часу обійшовся модальним вікном через портал, і для
 * списку на пів екрана це правильно. Але меню з трьох пунктів має стояти біля
 * своєї кнопки, а не посеред екрана, — тож тут звичайне абсолютне позиціювання
 * всередині relative-обгортки.
 *
 * Закриття зібрано в одному місці навмисно: клік поза межами, Escape і зміна
 * маршруту. Останнє легко забути, і тоді меню лишається висіти над уже іншою
 * сторінкою.
 */
export const Menu = ({
  trigger,
  label,
  align = "end",
  triggerClassName = "",
  className = "",
  children
}: Props) => {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const { pathname } = useLocation();

  // Перехід на іншу сторінку закриває меню: пункт, який щойно натиснули,
  // інакше лишався б на екрані вже над новим вмістом.
  useEffect(() => {
    setOpen(false);
  }, [pathname]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const onPointerDown = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
        // Фокус повертаємо на кнопку: інакше після Escape він лишається на
        // елементі, якого вже немає в документі.
        triggerRef.current?.focus();
      }
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);

    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  return (
    <div ref={containerRef} className="relative">
      <button
        ref={triggerRef}
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={label}
        onClick={() => setOpen((value) => !value)}
        className={triggerClassName}
      >
        {trigger}
      </button>

      {open && (
        <div
          role="menu"
          aria-label={label}
          className={`surface-raised absolute top-full z-50 mt-2 overflow-hidden py-1 ${
            align === "end" ? "right-0" : "left-0"
          } ${className}`}
        >
          {children(() => setOpen(false))}
        </div>
      )}
    </div>
  );
};

/** Спільний вигляд пункта меню — і для посилання, і для кнопки-дії. */
export const menuItemClass =
  "flex w-full items-center gap-2.5 px-3 py-2 text-left text-micro text-text-muted transition hover:bg-ink-700 hover:text-text";

export const MenuSeparator = () => <div className="my-1 border-t border-line-soft" />;
