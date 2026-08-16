const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5274";

const SIZES = {
  sm: "h-7 w-7 text-[0.625rem]",
  md: "h-8 w-8 text-micro",
  lg: "h-20 w-20 text-body"
} as const;

type Props = {
  url?: string | null;
  /** Ініціали, які показуємо, поки аватара немає. */
  fallback: string;
  size?: keyof typeof SIZES;
  alt?: string;
};

/**
 * Аватар користувача з відкатом на ініціали.
 *
 * Файли віддає API, а не dev-сервер Vite, тож відносний шлях доводиться
 * доклеювати до базового URL бекенда.
 */
export const Avatar = ({ url, fallback, size = "md", alt = "" }: Props) => {
  const classes = `${SIZES[size]} shrink-0 rounded-full object-cover`;

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
