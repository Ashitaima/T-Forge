import { choiceLabel, choicePortraitUrl } from "../../../constants/draft";

type Props = {
  game: string;
  action: string;
  values: string[];
  takenValues: Set<string>;
  canAct: boolean;
  busyValue: string | null;
  onChoose: (value: string) => void;
};

/**
 * Сітка вибору.
 *
 * Забране показуємо перекресленим, а не ховаємо: драфт читають і збоку, і
 * плитка, що зникла, не сказала б, що саме зникло.
 */
export const DraftBoard = ({
  game,
  action,
  values,
  takenValues,
  canAct,
  busyValue,
  onChoose
}: Props) => (
  <div className="grid grid-cols-3 gap-2 sm:grid-cols-4 lg:grid-cols-6">
    {values.map((value) => {
      const isTaken = takenValues.has(value);
      const portrait = choicePortraitUrl(game, action, value);
      const isBusy = busyValue === value;

      return (
        <button
          key={value}
          type="button"
          disabled={!canAct || isTaken || busyValue !== null}
          onClick={() => onChoose(value)}
          className={`surface-raised flex flex-col items-center gap-1 px-2 py-2 text-micro transition ${
            isTaken ? "line-through opacity-35" : "hover:bg-ink-700"
          } ${canAct && !isTaken ? "cursor-pointer" : "cursor-not-allowed"} ${
            isBusy ? "border-ember/50" : ""
          }`}
        >
          {portrait && (
            <img
              src={portrait}
              alt=""
              loading="lazy"
              className="h-12 w-12 rounded-md object-cover"
              // Зовнішній CDN, що відмовив, псує вигляд, але не ламає драфт:
              // лишається підпис.
              onError={(event) => {
                event.currentTarget.style.display = "none";
              }}
            />
          )}
          <span className="w-full truncate text-center">{choiceLabel(game, action, value)}</span>
        </button>
      );
    })}
  </div>
);
