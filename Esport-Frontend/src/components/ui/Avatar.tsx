const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5274";

const SIZES = {
  sm: "h-7 w-7 text-[0.625rem]",
  md: "h-8 w-8 text-micro",
  lg: "h-20 w-20 text-body"
} as const;

const SHAPES = {
  circle: "rounded-full",
  square: "rounded-md"
} as const;

type Props = {
  url?: string | null;
  /** Ініціали, які показуємо, поки аватара немає. */
  fallback: string;
  size?: keyof typeof SIZES;
  alt?: string;
  /** Логотип команди краще читається квадратним, аватар — круглим. */
  shape?: keyof typeof SHAPES;
};

/**
 * Аватар користувача з відкатом на ініціали.
 *
 * Файли віддає API, а не dev-сервер Vite, тож відносний шлях доводиться
 * доклеювати до базового URL бекенда.
 */
export const Avatar = ({ url, fallback, size = "md", alt = "", shape = "circle" }: Props) => {
  const classes = `${SIZES[size]} ${SHAPES[shape]} shrink-0 object-cover`;

  if (url) {
    const src = url.startsWith("http") ? url : `${API_BASE_URL}${url}`;
    return <img src={src} alt={alt} className={`${classes} border border-line`} />;
  }

  return (
    <span
      className={`${classes} flex items-center justify-center bg-ink-700 font-semibold uppercase text-text`}
      aria-hidden={alt === ""}
    >
      {fallback}
    </span>
  );
};

/** Запасні ініціали команди: тег, а якщо його немає — перша літера назви. */
export const teamInitials = (name?: string | null, tag?: string | null) =>
  (tag?.trim() || name?.trim()?.[0] || "?").slice(0, 3);
