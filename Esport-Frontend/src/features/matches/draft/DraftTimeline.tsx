import { Timer } from "lucide-react";
import { ACTION_LABELS, choiceLabel } from "../../../constants/draft";
import type { DraftDto } from "../../../types";

type Props = {
  draft: DraftDto;
  teamName: (teamId: number | null) => string;
};

/**
 * Смуга кроків: що вже зроблено, чий зараз хід, що буде далі.
 *
 * Саме заради неї кроки створюються всі одразу — інакше показати «далі бан
 * суперника, потім твій пік» було б нічим.
 */
export const DraftTimeline = ({ draft, teamName }: Props) => (
  <ol className="flex flex-wrap gap-2">
    {draft.steps.map((step) => {
      const isCurrent = step.stepIndex === draft.currentStepIndex;
      const isDone = step.chosenValue !== null;

      return (
        <li
          key={step.stepIndex}
          className={`surface-raised min-w-[7.5rem] px-3 py-2 ${
            isCurrent ? "border-ember/50 bg-ember/10" : ""
          } ${isDone ? "" : "opacity-70"}`}
        >
          <div className="eyebrow flex items-center gap-1.5">
            {ACTION_LABELS[step.action] ?? step.action}
            {step.wasAutoPicked && (
              // Бан, який зробив таймер, — не те саме, що бан капітана.
              <Timer className="h-3 w-3 text-text-faint" aria-label="Обрав таймер" />
            )}
          </div>
          <div className="truncate text-micro text-text">
            {isDone ? choiceLabel(draft.game, step.action, step.chosenValue!) : "—"}
          </div>
          <div className="truncate text-micro text-text-faint">{teamName(step.actingTeamId)}</div>
        </li>
      );
    })}
  </ol>
);
