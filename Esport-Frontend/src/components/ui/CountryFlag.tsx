import { countryFlag, countryName, isKnownCountry } from "../../constants/countries";

type Props = {
  /** Код ISO 3166-1 alpha-2 із бекенда. Може бути й невідомим (старі дані). */
  code: string | null | undefined;
  /** Показувати назву поруч. У щільних таблицях вимикається. */
  withName?: boolean;
  className?: string;
};

/**
 * Прапор країни.
 *
 * Емодзі, а не картинка: сімдесят вбудованих SVG роздули б збірку, а зовнішній
 * спрайт додав би залежність від мережі. Windows не має гліфів прапорів і
 * покаже замість них пару літер коду — це лишається читабельним, тим паче що
 * повна назва все одно є в title.
 *
 * Невідомий код (профіль, створений до переходу на коди ISO) показується
 * як є, без прапора — стирати чужі дані заради охайності не варто.
 */
export const CountryFlag = ({ code, withName = false, className = "" }: Props) => {
  if (!code) {
    return <span className="text-text-faint">—</span>;
  }

  const name = countryName(code);
  const flag = countryFlag(code);

  return (
    <span className={`inline-flex items-center gap-1.5 ${className}`} title={name}>
      {isKnownCountry(code) ? (
        <span aria-hidden className="text-[1.0625rem] leading-none">
          {flag}
        </span>
      ) : null}
      <span className={withName ? "" : "font-mono text-micro"}>{withName ? name : code}</span>
    </span>
  );
};
