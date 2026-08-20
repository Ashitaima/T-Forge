import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Swords } from "lucide-react";
import { matchChallengesApi } from "../../api/matchChallengesApi";
import { teamsApi } from "../../api/teamsApi";
import { useAuthStore } from "../../store/authStore";
import { useSubmitError } from "../../hooks/useSubmitError";
import { EmptyState, Skeleton } from "../../components/ui/Primitives";
import { DateTimePicker } from "../../components/ui/DateTimePicker";
import { GAMES, GAME_LABELS, gameLabel } from "../../constants/games";
import {
  MATCH_CHALLENGE_STATUS,
  MATCH_CHALLENGE_STATUS_PILL,
  matchChallengeStatusLabel
} from "../../constants/matchChallengeStatuses";
import type { MatchChallengeDto, TeamRowDto } from "../../types";

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("uk-UA", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit"
  });

type Props = {
  /** Команда, зі сторінки якої відкрито панель. */
  teamId: number;
  /** Акаунт капітана цієї команди — з ним звіряємо право викликати. */
  teamCaptainId: number | null;
};

/**
 * Виклики команди на товариський матч.
 *
 * Кнопки повторюють Common/MatchChallengePolicy.cs: на адресний виклик
 * відповідає лише капітан викликаної команди, на відкритий — капітан
 * будь-якої іншої, а скасовує лише той, хто надіслав. Сервер усе одно
 * перевіряє те саме — тут ми просто не показуємо того, що дасть 403.
 */
export const TeamChallengesPanel = ({ teamId, teamCaptainId }: Props) => {
  const user = useAuthStore((state) => state.user);
  const [challenges, setChallenges] = useState<MatchChallengeDto[]>([]);
  const [open, setOpen] = useState<MatchChallengeDto[]>([]);
  const [teams, setTeams] = useState<TeamRowDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const submitError = useSubmitError();

  const [isOpenChallenge, setIsOpenChallenge] = useState(false);
  const [opponentTeamId, setOpponentTeamId] = useState("");
  const [game, setGame] = useState("");
  const [format, setFormat] = useState("BO1");
  const [proposedAt, setProposedAt] = useState("");
  const [message, setMessage] = useState("");

  // Капітанство — це звʼязок із командою, а не роль, тож звіряємо
  // справжній id: перегляд чужої ролі не має давати прав над цією командою.
  const isCaptain = user !== null && teamCaptainId !== null && user.id === teamCaptainId;

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [mine, openOnes] = await Promise.all([
        matchChallengesApi.getForTeam(teamId),
        matchChallengesApi.getOpen()
      ]);
      setChallenges(mine);
      // Власні відкриті виклики вже є у списку команди — другий раз їх
      // показувати нема сенсу.
      setOpen(openOnes.filter((item) => item.challengerTeamId !== teamId));
    } catch {
      setChallenges([]);
      setOpen([]);
    } finally {
      setLoading(false);
    }
  }, [teamId]);

  useEffect(() => {
    load();
  }, [load]);

  // Список суперників потрібен лише тому, хто справді може викликати.
  useEffect(() => {
    if (!isCaptain) {
      setTeams([]);
      return;
    }

    let isActive = true;

    teamsApi
      .getPaged({ page: 1, pageSize: 100, sortBy: "name", sortDirection: "asc" })
      .then((response) => isActive && setTeams(response.data))
      .catch(() => isActive && setTeams([]));

    return () => {
      isActive = false;
    };
  }, [isCaptain]);

  const run = async (action: () => Promise<unknown>) => {
    submitError.clear();
    try {
      await action();
      await load();
    } catch (error) {
      submitError.capture(error);
    }
  };

  const submit = async () => {
    if ((!isOpenChallenge && !opponentTeamId) || !game || !proposedAt) {
      return;
    }

    await run(() =>
      matchChallengesApi.create({
        challengerTeamId: teamId,
        // Відкритий виклик іде без суперника — його назве той, хто прийме.
        opponentTeamId: isOpenChallenge ? null : Number(opponentTeamId),
        game,
        format,
        proposedAt,
        message
      })
    );

    setCreating(false);
    setOpponentTeamId("");
    setMessage("");
  };

  const opponents = teams.filter((team) => team.id !== teamId);

  const row = (challenge: MatchChallengeDto, acceptAsMine: boolean) => {
    const pending = challenge.status === MATCH_CHALLENGE_STATUS.Pending;
    const awaitingMe = pending && !challenge.isOpen && challenge.opponentTeamId === teamId && isCaptain;
    const canCancel = pending && challenge.challengerTeamId === teamId && isCaptain;
    // Відкритий виклик приймає капітан будь-якої іншої команди — так само,
    // як вирішує MatchChallengePolicy.
    const openToMe = pending && challenge.isOpen && acceptAsMine && isCaptain;

    return (
      <div key={challenge.id} className="surface-raised px-4 py-3.5">
        <div className="flex flex-wrap items-center gap-3">
          <div className="tabular w-24 shrink-0 font-mono text-micro text-text-faint">
            {formatDateTime(challenge.proposedAt)}
          </div>

          <div className="flex min-w-0 flex-1 items-center gap-2">
            <span className="truncate text-body text-text">{challenge.challengerTeamName}</span>
            <Swords className="h-3.5 w-3.5 shrink-0 text-text-faint" />
            {challenge.opponentTeamName ? (
              <span className="truncate text-body text-text">{challenge.opponentTeamName}</span>
            ) : (
              <span className="truncate text-body text-text-faint">Суперника ще немає</span>
            )}
          </div>

          <span className="shrink-0 text-micro text-text-faint">{gameLabel(challenge.game)}</span>
          <span className="shrink-0 text-micro text-text-faint">{challenge.format}</span>
          {challenge.isOpen && pending && <span className="pill pill-neutral shrink-0">Відкритий</span>}
          <span
            className={`pill shrink-0 ${
              MATCH_CHALLENGE_STATUS_PILL[challenge.status] ?? "pill-neutral"
            }`}
          >
            {matchChallengeStatusLabel(challenge.status)}
          </span>

          <div className="row-actions ml-auto">
            {openToMe && (
              <button
                type="button"
                onClick={() => run(() => matchChallengesApi.accept(challenge.id, teamId))}
                className="btn btn-primary btn-sm"
              >
                Прийняти виклик
              </button>
            )}

            {awaitingMe && (
              <>
                <button
                  type="button"
                  onClick={() => run(() => matchChallengesApi.accept(challenge.id))}
                  className="btn btn-primary btn-sm"
                >
                  Прийняти
                </button>
                <button
                  type="button"
                  onClick={() => run(() => matchChallengesApi.decline(challenge.id))}
                  className="btn btn-ghost btn-sm"
                >
                  Відхилити
                </button>
              </>
            )}

            {canCancel && (
              <button
                type="button"
                onClick={() => run(() => matchChallengesApi.cancel(challenge.id))}
                className="btn btn-ghost btn-sm"
              >
                Скасувати
              </button>
            )}

            {challenge.matchId && (
              <Link to={`/matches/${challenge.matchId}`} className="btn btn-ghost btn-sm">
                До матчу
              </Link>
            )}
          </div>
        </div>

        {challenge.message && <p className="muted mt-2 text-micro">{challenge.message}</p>}
      </div>
    );
  };

  return (
    <div className="space-y-4">
      {isCaptain && (
        <div className="flex justify-end">
          <button
            type="button"
            onClick={() => setCreating((current) => !current)}
            className="btn btn-primary"
          >
            <Swords className="h-4 w-4" />
            {creating ? "Згорнути" : "Викликати на матч"}
          </button>
        </div>
      )}

      {creating && (
        <section className="panel panel-body space-y-4">
          {/* Відкритий виклик — оголошення на дошці: суперника не названо,
              і прийняти його може капітан будь-якої іншої команди. Закритий —
              адресний, як було. */}
          <div className="field">
            Кому
            <div className="mt-1.5 flex gap-1.5">
              {[
                { value: false, label: "Конкретній команді" },
                { value: true, label: "Відкритий виклик" }
              ].map(({ value, label }) => (
                <button
                  key={label}
                  type="button"
                  onClick={() => setIsOpenChallenge(value)}
                  aria-pressed={isOpenChallenge === value}
                  className={`rounded-lg px-3 py-1.5 text-micro transition ${
                    isOpenChallenge === value
                      ? "bg-ink-800 text-text"
                      : "text-text-muted hover:bg-ink-900 hover:text-text"
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          {isOpenChallenge ? (
            <p className="rounded-lg border border-line bg-ink-800/60 px-3 py-2.5 text-micro text-text-muted">
              Виклик побачать усі команди. Матч почнеться з тією, що прийме його першою.
            </p>
          ) : (
            <label className="field">
              Суперник
              <select
                value={opponentTeamId}
                onChange={(event) => setOpponentTeamId(event.target.value)}
                className="input"
              >
                <option value="">Оберіть команду</option>
                {opponents.map((team) => (
                  <option key={team.id} value={team.id}>
                    {team.name} ({team.tag})
                  </option>
                ))}
              </select>
            </label>
          )}

          <label className="field">
            Дисципліна
            <select value={game} onChange={(event) => setGame(event.target.value)} className="input">
              <option value="">Оберіть дисципліну</option>
              {GAMES.map((value) => (
                <option key={value} value={value}>
                  {GAME_LABELS[value]}
                </option>
              ))}
            </select>
          </label>

          <label className="field">
            Формат
            <select
              value={format}
              onChange={(event) => setFormat(event.target.value)}
              className="input"
            >
              {["BO1", "BO3", "BO5"].map((value) => (
                <option key={value} value={value}>
                  {value}
                </option>
              ))}
            </select>
          </label>

          <div className="field">
            Час
            <div className="mt-1.5">
              <DateTimePicker ariaLabel="Час матчу" value={proposedAt} onChange={setProposedAt} />
            </div>
          </div>

          <label className="field">
            Повідомлення
            <input
              type="text"
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              placeholder="Необовʼязково"
              className="input"
            />
          </label>

          {submitError.error && <div className="notice notice-error">{submitError.error}</div>}

          <div className="flex items-center gap-3 border-t border-line-soft pt-4">
            <button
              type="button"
              onClick={submit}
              disabled={(!isOpenChallenge && !opponentTeamId) || !game || !proposedAt}
              className="btn btn-primary"
            >
              {isOpenChallenge ? "Опублікувати виклик" : "Надіслати виклик"}
            </button>
            <button type="button" onClick={() => setCreating(false)} className="btn btn-ghost">
              Скасувати
            </button>
          </div>
        </section>
      )}

      {!creating && submitError.error && (
        <div className="notice notice-error">{submitError.error}</div>
      )}

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Виклики команди</h2>
        </div>
        <div className="panel-body space-y-3">
          {loading && <Skeleton rows={3} />}

          {!loading && challenges.length === 0 && (
            <EmptyState
              title="Викликів ще немає"
              hint="Товариський матч не дає ні титулів, ні рейтингу — це просто гра з іншою командою."
            />
          )}

          {!loading && challenges.map((challenge) => row(challenge, false))}
        </div>
      </section>

      {!loading && open.length > 0 && (
        <section className="panel">
          <div className="panel-header">
            <h2 className="section-title">Відкриті виклики</h2>
            <span className="muted text-micro">Суперника ще не названо</span>
          </div>
          <div className="panel-body space-y-3">
            {open.map((challenge) => row(challenge, true))}
          </div>
        </section>
      )}
    </div>
  );
};

export default TeamChallengesPanel;
