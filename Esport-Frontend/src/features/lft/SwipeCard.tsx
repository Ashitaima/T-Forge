import { useEffect } from "react";
import { Check, X } from "lucide-react";
import { Avatar } from "../../components/ui/Avatar";

type Props = {
  title: string;
  subtitle?: string;
  imageUrl?: string | null;
  /** Ініціали на випадок відсутнього зображення — той самий Avatar, що й усюди в застосунку. */
  imageFallback: string;
  facts: { label: string; value: string }[];
  body?: string;
  busy: boolean;
  onLike: () => void;
  onPass: () => void;
};

/**
 * Одна картка стрічки. Керується клавішами й кнопками, без перетягування:
 * це десктопний інтерфейс, а drag тягне за собою залежність і сенсорний
 * сценарій, якого тут ніхто не перевірить.
 *
 * ember лишається на кнопці згоди — це єдина дія, що змінює склад.
 */
export const SwipeCard = ({
  title, subtitle, imageUrl, imageFallback, facts, body, busy, onLike, onPass
}: Props) => {
  useEffect(() => {
    // Поки триває запит, клавіші мовчать: другий змах пішов би по картці,
    // якої вже немає.
    if (busy) {
      return;
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "ArrowLeft") {
        onPass();
      } else if (event.key === "ArrowRight") {
        onLike();
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [busy, onLike, onPass]);

  return (
    <article className="panel max-w-md">
      <div className="panel-body space-y-4">
        <div className="flex items-center gap-3">
          <Avatar url={imageUrl} fallback={imageFallback} shape="square" size="lg" alt="" />
          <div>
            <h3 className="section-title">{title}</h3>
            {subtitle && <p className="eyebrow">{subtitle}</p>}
          </div>
        </div>

        <dl className="flex flex-wrap gap-2">
          {facts.map((fact) => (
            <div key={fact.label} className="pill">
              <dt className="sr-only">{fact.label}</dt>
              <dd className="tabular">{fact.value}</dd>
            </div>
          ))}
        </dl>

        {body && <p className="text-micro text-text-faint">{body}</p>}

        <div className="flex gap-2">
          <button type="button" className="btn" disabled={busy} onClick={onPass}>
            <X size={14} /> Пропустити
          </button>
          <button type="button" className="btn btn-primary" disabled={busy} onClick={onLike}>
            <Check size={14} /> Цікаво
          </button>
        </div>

        <p className="text-micro text-text-faint">← пропустити · → цікаво</p>
      </div>
    </article>
  );
};

export default SwipeCard;
