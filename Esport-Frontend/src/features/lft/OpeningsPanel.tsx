import { useCallback, useEffect, useState } from "react";
import { Plus } from "lucide-react";
import { lftApi } from "../../api/lftApi";
import { gameLabel } from "../../constants/games";
import { positionLabel } from "../../constants/gamePositions";
import { rankLabel } from "../../constants/gameRanks";
import { offsetLabel } from "../../constants/timeZones";
import { useIsCaptainOf } from "../../hooks/useEffectiveRole";
import { useSubmitError } from "../../hooks/useSubmitError";
import {
  TEAM_OPENING_STATUS_PILL,
  isOpeningOpen,
  teamOpeningStatusLabel
} from "../../constants/teamOpeningStatuses";
import { EmptyState, Skeleton } from "../../components/ui/Primitives";
import { OpeningForm } from "./OpeningForm";
import type { TeamOpeningDto } from "../../types";

type Props = {
  teamId: number;
  captainId: number;
};

/**
 * Вакансії команди. Публікувати й закривати їх може лише капітан —
 * капітанство це колонка Team.CaptainId, а не роль, тож перевіряємо його
 * через useIsCaptainOf, а не через user.role: у режимі розробника роль
 * підмінена, і компонент, що читає її напряму, лишився б у реальній ролі.
 */
export const OpeningsPanel = ({ teamId, captainId }: Props) => {
  const [openings, setOpenings] = useState<TeamOpeningDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [adding, setAdding] = useState(false);
  const [busy, setBusy] = useState(false);
  const isCaptain = useIsCaptainOf(teamId, captainId);
  const submitError = useSubmitError();

  const load = useCallback(() => {
    setLoading(true);
    lftApi
      .openingsForTeam(teamId)
      .then(setOpenings)
      .catch(() => setOpenings([]))
      .finally(() => setLoading(false));
  }, [teamId]);

  useEffect(() => {
    load();
  }, [load]);

  const close = async (id: number) => {
    setBusy(true);
    submitError.clear();
    try {
      const closed = await lftApi.closeOpening(id);
      setOpenings((rows) => rows.map((row) => (row.id === id ? closed : row)));
    } catch (error) {
      submitError.capture(error, "Не вдалося закрити вакансію.");
    } finally {
      setBusy(false);
    }
  };

  /** «Sentinel, Ascendant 2 і вище, UTC+00:00…UTC+03:00» — те, чого вимагає вакансія. */
  const requirements = (opening: TeamOpeningDto) => {
    const parts = [
      opening.position ? positionLabel(opening.position) : "будь-яка роль",
      opening.minRank ? `${rankLabel(opening.game, opening.minRank)} і вище` : "будь-який ранг"
    ];
    if (opening.minUtcOffsetMinutes !== null || opening.maxUtcOffsetMinutes !== null) {
      parts.push(`${offsetLabel(opening.minUtcOffsetMinutes)}…${offsetLabel(opening.maxUtcOffsetMinutes)}`);
    }
    return parts.join(" · ");
  };

  return (
    <section className="panel">
      <div className="panel-header flex items-center justify-between">
        <h2 className="section-title">Вакансії у складі</h2>
        {isCaptain && !adding && (
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => setAdding(true)}>
            <Plus className="h-3.5 w-3.5" /> Нова вакансія
          </button>
        )}
      </div>

      {adding && (
        <OpeningForm
          teamId={teamId}
          onSaved={(opening) => {
            setOpenings((rows) => [opening, ...rows]);
            setAdding(false);
          }}
          onCancel={() => setAdding(false)}
        />
      )}

      <div className="panel-body space-y-3">
        {!adding && submitError.error && <p className="notice notice-error">{submitError.error}</p>}

        {loading && <Skeleton rows={2} />}

        {!loading && openings.length === 0 && (
          <EmptyState
            title="Команда зараз нікого не шукає"
            hint="Капітан може опублікувати вакансію, щоб гравці бачили її у стрічці підбору."
          />
        )}

        {!loading && openings.length > 0 && (
          <ul className="space-y-2">
            {openings.map((opening) => (
              <li key={opening.id} className="surface-raised flex items-start justify-between gap-3 p-3">
                <div className="min-w-0 space-y-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="pill">{gameLabel(opening.game)}</span>
                    <span className={`pill ${TEAM_OPENING_STATUS_PILL[opening.status] ?? "pill-neutral"}`}>
                      {teamOpeningStatusLabel(opening.status)}
                    </span>
                  </div>
                  <p className="text-micro text-text-faint">{requirements(opening)}</p>
                  {opening.pitch && <p className="text-micro text-text">{opening.pitch}</p>}
                </div>
                {isCaptain && isOpeningOpen(opening.status) && (
                  <button
                    type="button"
                    className="btn btn-ghost btn-sm shrink-0"
                    disabled={busy}
                    onClick={() => close(opening.id)}
                  >
                    Закрити
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
};

export default OpeningsPanel;
