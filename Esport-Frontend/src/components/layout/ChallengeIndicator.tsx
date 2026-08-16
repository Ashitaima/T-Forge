import { useCallback, useEffect, useState } from "react";
import { Swords, X } from "lucide-react";
import { matchChallengesApi } from "../../api/matchChallengesApi";
import { gameLabel } from "../../constants/games";
import type { MatchChallengeDto } from "../../types";

/** Дістає читабельне повідомлення з відповіді API. */
const readApiError = (error: unknown, fallback: string) => {
  const response = (error as { response?: { data?: { message?: string } } })?.response?.data;
  return response?.message ?? fallback;
};

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("uk-UA", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit"
  });

/**
 * Індикатор викликів, що чекають на відповідь поточного користувача.
 *
 * Список перечитується під час монтування і після кожної дії — без опитування
 * сервера за таймером, так само як панелі запитів на членство. Виклик, надісланий
 * поки сторінка відкрита, зʼявиться після наступного переходу.
 */
export const ChallengeIndicator = () => {
  const [challenges, setChallenges] = useState<MatchChallengeDto[]>([]);
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    const rows = await matchChallengesApi.getPending().catch(() => [] as MatchChallengeDto[]);
    setChallenges(rows);
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const act = async (action: "accept" | "decline", id: number) => {
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

  useEffect(() => {
    if (challenges.length === 0) {
      setOpen(false);
    }
  }, [challenges.length]);

  if (challenges.length === 0) {
    return null;
  }

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex w-full items-center gap-2 rounded-lg border border-ember/40 bg-ember/10 px-3 py-2 text-micro text-text transition hover:bg-ember/15"
      >
        <Swords className="h-4 w-4 text-ember" />
        <span className="flex-1 text-left">Виклики на матч</span>
        <span className="tabular font-mono text-ember">{challenges.length}</span>
      </button>

      {open && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-ink-950/70 p-4"
          role="dialog"
          aria-modal="true"
          aria-label="Виклики на матч"
        >
          <div className="panel max-h-[80vh] w-full max-w-lg overflow-y-auto">
            <div className="panel-header">
              <h2 className="section-title">Виклики на матч</h2>
              <button
                type="button"
                onClick={() => setOpen(false)}
                className="btn btn-ghost btn-sm px-2"
                aria-label="Закрити"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="panel-body space-y-3">
              {error && <div className="notice notice-error">{error}</div>}

              {challenges.map((challenge) => (
                <div key={challenge.id} className="surface-raised px-4 py-3.5">
                  <div className="text-body text-text">
                    «{challenge.challengerTeamName}» викликає вас на матч
                  </div>
                  <div className="muted mt-1.5 flex flex-wrap items-center gap-2 text-micro">
                    <span className="pill">{gameLabel(challenge.game)}</span>
                    <span className="font-mono">{challenge.format}</span>
                    <span className="tabular font-mono">{formatDateTime(challenge.proposedAt)}</span>
                  </div>
                  {challenge.message && (
                    <div className="muted mt-1.5 text-micro">{challenge.message}</div>
                  )}
                  <div className="mt-3 flex gap-2">
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => act("accept", challenge.id)}
                      className="btn btn-primary btn-sm"
                    >
                      Прийняти
                    </button>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => act("decline", challenge.id)}
                      className="btn btn-ghost btn-sm"
                    >
                      Відхилити
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default ChallengeIndicator;
