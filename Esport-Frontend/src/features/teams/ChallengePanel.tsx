import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { matchChallengesApi } from "../../api/matchChallengesApi";
import { teamsApi } from "../../api/teamsApi";
import { GAMES, GAME_LABELS, gameLabel } from "../../constants/games";
import type { MatchChallengeDto, TeamRowDto } from "../../types";

const FORMATS = ["BO1", "BO3", "BO5"] as const;

/** Дістає читабельне повідомлення з відповіді API. */
const readApiError = (error: unknown, fallback: string) => {
  const response = (error as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } })
    ?.response?.data;
  const validationErrors = response?.errors ? Object.values(response.errors).flat().join(" ") : null;
  return validationErrors ?? response?.message ?? fallback;
};

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("uk-UA", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit"
  });

/** Значення для input[type=datetime-local] — рівно за годину від зараз. */
const defaultProposedAt = () => {
  const when = new Date(Date.now() + 60 * 60 * 1000);
  const offset = when.getTimezoneOffset() * 60 * 1000;
  return new Date(when.getTime() - offset).toISOString().slice(0, 16);
};

type Props = {
  teamId: number;
  isCaptain: boolean;
  isAdmin: boolean;
};

/**
 * Виклики на товариські матчі для однієї команди.
 * Побудовано за зразком панелей запитів на членство у TeamDetail:
 * вхідні — з кнопками відповіді, вихідні — з кнопкою скасування.
 */
export const ChallengePanel = ({ teamId, isCaptain, isAdmin }: Props) => {
  const [challenges, setChallenges] = useState<MatchChallengeDto[]>([]);
  const [teams, setTeams] = useState<TeamRowDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [opponentTeamId, setOpponentTeamId] = useState("");
  const [game, setGame] = useState<string>(GAMES[0]);
  const [proposedAt, setProposedAt] = useState(defaultProposedAt);
  const [format, setFormat] = useState<string>("BO1");
  const [message, setMessage] = useState("");

  const canManage = isCaptain || isAdmin;

  const load = useCallback(async () => {
    const rows = await matchChallengesApi.getForTeam(teamId).catch(() => [] as MatchChallengeDto[]);
    setChallenges(rows);
  }, [teamId]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!canManage) {
      return;
    }

    let isActive = true;

    teamsApi
      .getPaged({ page: 1, pageSize: 100 })
      .then((response) => isActive && setTeams(response.data))
      .catch(() => isActive && setTeams([]));

    return () => {
      isActive = false;
    };
  }, [canManage]);

  const pending = challenges.filter((challenge) => challenge.status === "Pending");
  const incoming = pending.filter((challenge) => challenge.opponentTeamId === teamId);
  const outgoing = pending.filter((challenge) => challenge.challengerTeamId === teamId);
  const history = challenges.filter((challenge) => challenge.status !== "Pending");

  const act = async (action: "accept" | "decline" | "cancel", id: number) => {
    setError(null);
    setBusy(true);
    try {
      await matchChallengesApi[action](id);
      await load();
    } catch (caught) {
      setError(readApiError(caught, "Не вдалося виконати дію."));
    } finally {
      setBusy(false);
    }
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);

    if (!opponentTeamId) {
      setError("Оберіть команду-суперника.");
      return;
    }

    // Сервер теж це перевіряє, але хай користувач дізнається одразу.
    if (new Date(proposedAt).getTime() <= Date.now()) {
      setError("Час матчу має бути в майбутньому.");
      return;
    }

    setBusy(true);
    try {
      await matchChallengesApi.create({
        challengerTeamId: teamId,
        opponentTeamId: Number(opponentTeamId),
        game,
        proposedAt: new Date(proposedAt).toISOString(),
        format,
        message
      });
      setOpponentTeamId("");
      setMessage("");
      setProposedAt(defaultProposedAt());
      await load();
    } catch (caught) {
      setError(readApiError(caught, "Не вдалося надіслати виклик."));
    } finally {
      setBusy(false);
    }
  };

  const ChallengeRow = ({
    challenge,
    children
  }: {
    challenge: MatchChallengeDto;
    children?: React.ReactNode;
  }) => (
    <div className="surface-raised mt-3 flex flex-wrap items-center justify-between gap-3 px-4 py-3.5">
      <div className="min-w-0">
        <div className="truncate text-body font-medium text-text">
          {challenge.challengerTeamName} <span className="text-text-faint">проти</span>{" "}
          {challenge.opponentTeamName}
        </div>
        <div className="muted mt-1 flex flex-wrap items-center gap-2 text-micro">
          <span className="pill">{gameLabel(challenge.game)}</span>
          <span className="font-mono">{challenge.format}</span>
          <span className="tabular font-mono">{formatDateTime(challenge.proposedAt)}</span>
        </div>
        {challenge.message && <div className="muted mt-1 text-micro">{challenge.message}</div>}
      </div>
      <div className="flex shrink-0 items-center gap-2">{children}</div>
    </div>
  );

  return (
    <section className="panel mt-6">
      <div className="eyebrow">Виклики на товариський матч</div>

      {error && <div className="notice notice-error mt-3">{error}</div>}

      {incoming.length === 0 && outgoing.length === 0 && (
        <p className="muted mt-3 text-micro">Відкритих викликів немає.</p>
      )}

      {incoming.map((challenge) => (
        <ChallengeRow key={challenge.id} challenge={challenge}>
          {canManage ? (
            <>
              <button
                type="button"
                disabled={busy}
                className="btn btn-primary btn-sm"
                onClick={() => act("accept", challenge.id)}
              >
                Прийняти
              </button>
              <button
                type="button"
                disabled={busy}
                className="btn btn-ghost btn-sm"
                onClick={() => act("decline", challenge.id)}
              >
                Відхилити
              </button>
            </>
          ) : (
            <span className="pill">Очікує відповіді</span>
          )}
        </ChallengeRow>
      ))}

      {outgoing.map((challenge) => (
        <ChallengeRow key={challenge.id} challenge={challenge}>
          <span className="pill">Надіслано</span>
          {canManage && (
            <button
              type="button"
              disabled={busy}
              className="btn btn-ghost btn-sm"
              onClick={() => act("cancel", challenge.id)}
            >
              Скасувати
            </button>
          )}
        </ChallengeRow>
      ))}

      {history.length > 0 && (
        <div className="mt-5 border-t border-line-soft pt-4">
          <div className="eyebrow">Історія викликів</div>
          {history.slice(0, 5).map((challenge) => (
            <ChallengeRow key={challenge.id} challenge={challenge}>
              {challenge.status === "Accepted" && challenge.matchId ? (
                <Link to={`/matches/${challenge.matchId}`} className="btn btn-ghost btn-sm">
                  До матчу
                </Link>
              ) : (
                <span className="pill">
                  {challenge.status === "Declined" ? "Відхилено" : "Скасовано"}
                </span>
              )}
            </ChallengeRow>
          ))}
        </div>
      )}

      {canManage && (
        <form onSubmit={submit} className="mt-5 space-y-4 border-t border-line-soft pt-4">
          <div className="eyebrow">Викликати команду</div>

          <label className="field">
            Суперник
            <select
              value={opponentTeamId}
              onChange={(event) => setOpponentTeamId(event.target.value)}
              className="input"
            >
              <option value="">Оберіть команду</option>
              {teams
                .filter((team) => team.id !== teamId)
                .map((team) => (
                  <option key={team.id} value={team.id}>
                    {team.name} ({team.tag})
                  </option>
                ))}
            </select>
          </label>

          <div className="grid gap-4 sm:grid-cols-2">
            <label className="field">
              Дисципліна
              <select value={game} onChange={(event) => setGame(event.target.value)} className="input">
                {GAMES.map((value) => (
                  <option key={value} value={value}>
                    {GAME_LABELS[value]}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              Формат
              <select value={format} onChange={(event) => setFormat(event.target.value)} className="input">
                {FORMATS.map((value) => (
                  <option key={value} value={value}>
                    {value}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <label className="field">
            Час матчу
            <input
              type="datetime-local"
              value={proposedAt}
              onChange={(event) => setProposedAt(event.target.value)}
              className="input"
            />
          </label>

          <label className="field">
            Повідомлення
            <textarea
              rows={2}
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              maxLength={300}
              className="input"
            />
          </label>

          <button type="submit" disabled={busy} className="btn btn-primary btn-sm">
            {busy ? "Надсилання..." : "Надіслати виклик"}
          </button>
        </form>
      )}
    </section>
  );
};

export default ChallengePanel;
