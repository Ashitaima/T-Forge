import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { tournamentInvitationsApi } from "../../api/tournamentInvitationsApi";
import { gameLabel } from "../../constants/games";
import type { TournamentInvitationDto } from "../../types";

/** Дістає читабельне повідомлення з відповіді API. */
const readApiError = (error: unknown, fallback: string) => {
  const response = (error as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } })
    ?.response?.data;
  const validationErrors = response?.errors ? Object.values(response.errors).flat().join(" ") : null;
  return validationErrors ?? response?.message ?? fallback;
};

type Props = {
  /** Одне з двох: панель команди або панель турніру. */
  teamId: number;
  /** Чи має поточний користувач права капітана цієї команди. */
  isCaptain: boolean;
};

/**
 * Запрошення й заявки команди на турніри — з боку команди.
 * Побудовано за зразком ChallengePanel: вхідні з кнопками відповіді,
 * вихідні з кнопкою скасування.
 */
export const TournamentInvitationsPanel = ({ teamId, isCaptain }: Props) => {
  const [rows, setRows] = useState<TournamentInvitationDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    const data = await tournamentInvitationsApi
      .getForTeam(teamId)
      .catch(() => [] as TournamentInvitationDto[]);
    setRows(data);
  }, [teamId]);

  useEffect(() => {
    load();
  }, [load]);

  const pending = rows.filter((row) => row.status === "Pending");
  const invitations = pending.filter((row) => row.direction === "Invite");
  const applications = pending.filter((row) => row.direction === "Application");

  const act = async (action: "accept" | "decline" | "cancel", id: number) => {
    setError(null);
    setBusy(true);
    try {
      await tournamentInvitationsApi[action](id);
      await load();
    } catch (caught) {
      setError(readApiError(caught, "Не вдалося виконати дію."));
    } finally {
      setBusy(false);
    }
  };

  // Нічого не чекає відповіді — панель мовчить, а не займає екран порожнім блоком.
  if (invitations.length === 0 && applications.length === 0) {
    return null;
  }

  const Row = ({
    invitation,
    children
  }: {
    invitation: TournamentInvitationDto;
    children?: React.ReactNode;
  }) => (
    <div className="surface-raised mt-3 flex flex-wrap items-center justify-between gap-3 px-4 py-3.5">
      <div className="min-w-0">
        <Link
          to={`/tournaments/${invitation.tournamentId}`}
          className="truncate text-body font-medium text-text hover:text-ember"
        >
          {invitation.tournamentName}
        </Link>
        <div className="muted mt-1 flex flex-wrap items-center gap-2 text-micro">
          <span className="pill">{gameLabel(invitation.tournamentGame)}</span>
        </div>
        {invitation.message && <div className="muted mt-1 text-micro">{invitation.message}</div>}
      </div>
      <div className="flex shrink-0 items-center gap-2">{children}</div>
    </div>
  );

  return (
    <section className="panel mt-6">
      <div className="eyebrow">Запрошення на турніри</div>

      {error && <div className="notice notice-error mt-3">{error}</div>}

      {invitations.map((invitation) => (
        <Row key={invitation.id} invitation={invitation}>
          {isCaptain ? (
            <>
              <button
                type="button"
                disabled={busy}
                className="btn btn-primary btn-sm"
                onClick={() => act("accept", invitation.id)}
              >
                Прийняти
              </button>
              <button
                type="button"
                disabled={busy}
                className="btn btn-ghost btn-sm"
                onClick={() => act("decline", invitation.id)}
              >
                Відхилити
              </button>
            </>
          ) : (
            <span className="pill">Очікує відповіді капітана</span>
          )}
        </Row>
      ))}

      {applications.map((invitation) => (
        <Row key={invitation.id} invitation={invitation}>
          <span className="pill">Заявку надіслано</span>
          {isCaptain && (
            <button
              type="button"
              disabled={busy}
              className="btn btn-ghost btn-sm"
              onClick={() => act("cancel", invitation.id)}
            >
              Скасувати
            </button>
          )}
        </Row>
      ))}
    </section>
  );
};

export default TournamentInvitationsPanel;
