import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { AlertCircle, BarChart3, Minus, Plus, Radio, Trash2, UserPlus } from "lucide-react";
import { matchesApi } from "../../api/matchesApi";
import { ratingsApi } from "../../api/ratingsApi";
import { subscribeToMatch } from "../../api/matchHub";
import { useIsCaptainOf, useIsRole } from "../../hooks/useEffectiveRole";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Skeleton, StatusPill } from "../../components/ui/Primitives";
import { RatingDelta } from "../../components/ui/Rating";
import { Avatar, teamInitials } from "../../components/ui/Avatar";
import type { MatchDto, MatchPlayerDto, MatchRatingDeltaDto } from "../../types";

const MatchDetail = () => {
  const { id } = useParams();
  const matchId = Number(id);
  const isStaff = useIsRole("Admin", "Organizer");
  const { user } = useAuthStore();

  const [match, setMatch] = useState<MatchDto | null>(null);
  const [roster, setRoster] = useState<MatchPlayerDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [liveConnected, setLiveConnected] = useState(false);
  const [streamDraft, setStreamDraft] = useState("");
  const [trackerDraft, setTrackerDraft] = useState("");
  const [linksSaved, setLinksSaved] = useState(false);
  const [winnerChoice, setWinnerChoice] = useState<string>("");
  const [finishTracker, setFinishTracker] = useState("");
  const [ratingDelta, setRatingDelta] = useState<MatchRatingDeltaDto | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [matchData, rosterData, deltaData] = await Promise.all([
        matchesApi.getById(matchId),
        matchesApi.getRoster(matchId).catch(() => [] as MatchPlayerDto[]),
        // Товариський або ще не зіграний матч рейтингу не має — це
        // нормальний стан, а не помилка.
        ratingsApi.getMatchDelta(matchId).catch(() => null)
      ]);
      setMatch(matchData);
      setStreamDraft(matchData.streamUrl ?? "");
      setTrackerDraft(matchData.trackerUrl ?? "");
      setRoster(rosterData);
      setRatingDelta(deltaData);
    } finally {
      setLoading(false);
    }
  }, [matchId]);

  useEffect(() => {
    load();
  }, [load]);

  // Живі оновлення рахунку й статусу
  useEffect(() => {
    if (!matchId) {
      return;
    }

    setLiveConnected(true);
    const unsubscribe = subscribeToMatch(matchId, {
      onScore: (update) =>
        setMatch((prev) =>
          prev
            ? { ...prev, homeTeamScore: update.homeTeamScore, awayTeamScore: update.awayTeamScore }
            : prev
        ),
      onStatus: (update) => {
        setMatch((prev) =>
          prev
            ? {
                ...prev,
                status: update.status,
                homeTeamScore: update.homeTeamScore,
                awayTeamScore: update.awayTeamScore
              }
            : prev
        );
        // Завершення матчу змінює й переможця, і склад сітки — перечитуємо
        if (update.status === "Completed") {
          load();
        }
      }
    });

    return () => {
      setLiveConnected(false);
      unsubscribe();
    };
  }, [matchId, load]);

  const run = async (action: () => Promise<unknown>) => {
    setBusy(true);
    setError(null);
    try {
      await action();
    } catch (err: unknown) {
      const response = (err as { response?: { data?: { message?: string } } }).response;
      setError(response?.data?.message ?? "Не вдалося виконати дію");
    } finally {
      setBusy(false);
    }
  };

  const adjustScore = (homeDelta: number, awayDelta: number) => {
    if (!match) {
      return;
    }
    const home = Math.max(0, match.homeTeamScore + homeDelta);
    const away = Math.max(0, match.awayTeamScore + awayDelta);
    // Оптимістично: сокет усе одно надішле підтверджене значення
    setMatch({ ...match, homeTeamScore: home, awayTeamScore: away });
    run(() => matchesApi.updateScore(matchId, { homeTeamScore: home, awayTeamScore: away }));
  };

  // Товариський матч народжується з виклику двох капітанів, організатора в
  // ньому немає — тож вести його можуть самі капітани. Дзеркалить серверний
  // FriendlyMatchPolicy; сервер усе одно перевіряє це сам.
  const isFriendly = match?.tournamentId === null;
  // Через хук, щоб діяв режим розробника: адміністратор може подивитися на
  // матч очима капітана однієї з команд.
  const isHomeCaptain = useIsCaptainOf(match?.homeTeam?.id, match?.homeTeamCaptainId);
  const isAwayCaptain = useIsCaptainOf(match?.awayTeam?.id, match?.awayTeamCaptainId);
  const isCaptainHere = Boolean(user) && (isHomeCaptain || isAwayCaptain);
  const canScore = isStaff || (isFriendly && isCaptainHere);

  const isLive = match?.status === "InProgress";

  const [homeRoster, awayRoster] = useMemo(() => {
    const home = roster.filter((entry) => entry.teamId === match?.homeTeam?.id);
    const away = roster.filter((entry) => entry.teamId === match?.awayTeam?.id);
    return [home, away];
  }, [roster, match?.homeTeam?.id, match?.awayTeam?.id]);

  const updateEntry = (entry: MatchPlayerDto, patch: Partial<MatchPlayerDto>) => {
    const next = { ...entry, ...patch };
    setRoster((prev) => prev.map((item) => (item.id === entry.id ? next : item)));
    run(() =>
      matchesApi.updateRosterPlayer(matchId, entry.id, {
        kills: next.kills,
        deaths: next.deaths,
        assists: next.assists,
        champion: next.champion,
        isStarter: next.isStarter
      })
    );
  };

  const RosterTable = ({ title, entries }: { title: string; entries: MatchPlayerDto[] }) => (
    <div>
      <h3 className="eyebrow mb-3">{title}</h3>
      {entries.length === 0 ? (
        <p className="muted text-body">Немає гравців.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>Гравець</th>
                <th className="text-right">В</th>
                <th className="text-right">С</th>
                <th className="text-right">А</th>
                <th>Персонаж</th>
                {canScore && <th className="w-px" />}
              </tr>
            </thead>
            <tbody>
              {entries.map((entry) => (
                <tr key={entry.id}>
                  <td className="cell-primary">
                    {entry.player?.nickname ?? "—"}
                    {!entry.isStarter && <span className="ml-2 text-micro text-text-faint">заміна</span>}
                  </td>
                  {(["kills", "deaths", "assists"] as const).map((stat) => (
                    <td key={stat} className="text-right">
                      {canScore ? (
                        <input
                          type="number"
                          min={0}
                          value={entry[stat]}
                          onChange={(event) =>
                            updateEntry(entry, { [stat]: Math.max(0, Number(event.target.value)) })
                          }
                          className="input tabular w-16 px-2 text-right font-mono"
                          style={{ height: "1.875rem" }}
                          aria-label={`${entry.player?.nickname} ${stat}`}
                        />
                      ) : (
                        <span className="tabular font-mono">{entry[stat]}</span>
                      )}
                    </td>
                  ))}
                  <td>
                    {canScore ? (
                      <input
                        type="text"
                        value={entry.champion}
                        onChange={(event) => updateEntry(entry, { champion: event.target.value })}
                        className="input w-28"
                        style={{ height: "1.875rem" }}
                        aria-label={`${entry.player?.nickname} персонаж`}
                      />
                    ) : (
                      entry.champion || "—"
                    )}
                  </td>
                  {canScore && (
                    <td>
                      <div className="row-actions">
                        <button
                          type="button"
                          disabled={busy}
                          onClick={() =>
                            run(async () => {
                              await matchesApi.removeRosterPlayer(matchId, entry.id);
                              setRoster((prev) => prev.filter((item) => item.id !== entry.id));
                            })
                          }
                          className="btn btn-ghost btn-sm px-2 text-text-faint hover:text-danger"
                          aria-label="Прибрати з ростера"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );

  return (
    <>
      <PageHeader
        eyebrow={
          match?.tournamentId === null ? "Товариський матч" : (match?.tournament?.name ?? "Матч")
        }
        title={`${match?.homeTeam?.name ?? "Очікується"} — ${match?.awayTeam?.name ?? "Очікується"}`}
        description={match ? `${match.matchType} · ${match.format}` : undefined}
        action={
          <div className="flex items-center gap-2">
            {/* Тільки поки матч іде — див. коментар у MatchesSchedule. */}
            {match?.streamUrl && isLive && (
              <a
                href={match.streamUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="btn btn-primary"
              >
                <Radio className="h-4 w-4" />
                Дивитися
              </a>
            )}
            {match?.trackerUrl && (
              <a
                href={match.trackerUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="btn btn-secondary"
              >
                <BarChart3 className="h-4 w-4" />
                Статистика матчу
              </a>
            )}
            {canScore && (
              <Link to={`/matches/${matchId}/edit`} className="btn btn-secondary">
                Редагувати
              </Link>
            )}
          </div>
        }
      />

      {error && (
        <div className="notice notice-error">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {loading && <Skeleton rows={2} />}

      {!loading && match && (
        <>
          {/* Табло. Під час матчу рахунок приходить сокетом, а не з перезавантаження */}
          <section className={`panel ${isLive ? "border-ember/40" : ""}`}>
            <div className="panel-header">
              <div className="flex items-center gap-3">
                <StatusPill status={match.status} />
                {isLive && liveConnected && (
                  <span className="flex items-center gap-1.5 text-micro text-ember">
                    <Radio className="h-3.5 w-3.5 animate-heat-pulse" />
                    наживо
                  </span>
                )}
              </div>
              <span className="tabular font-mono text-micro text-text-faint">
                {new Date(match.scheduledAt).toLocaleString("uk-UA")}
              </span>
            </div>

            <div className="panel-body">
              <div className="grid items-center gap-4 sm:grid-cols-[1fr_auto_1fr]">
                <div className="text-center sm:text-right">
                  <div className="flex items-center justify-center gap-2.5 sm:justify-end">
                    <div className="text-lead font-medium text-text">{match.homeTeam?.name ?? "Очікується"}</div>
                    <Avatar
                      url={match.homeTeam?.logoPath}
                      shape="square"
                      size="md"
                      fallback={teamInitials(match.homeTeam?.name, match.homeTeam?.tag)}
                      alt=""
                    />
                  </div>
                  <div className="flex items-center justify-center gap-2 sm:justify-end">
                    <span className="font-mono text-micro text-text-faint">{match.homeTeam?.tag}</span>
                    <RatingDelta delta={ratingDelta?.homeDelta} />
                  </div>
                </div>

                <div className="tabular flex items-center justify-center gap-4 font-mono text-[2.5rem] leading-none text-text">
                  <span className={match.winnerTeam?.id === match.homeTeam?.id ? "text-win" : ""}>
                    {match.homeTeamScore}
                  </span>
                  <span className="text-text-faint">:</span>
                  <span className={match.winnerTeam?.id === match.awayTeam?.id ? "text-win" : ""}>
                    {match.awayTeamScore}
                  </span>
                </div>

                <div className="text-center sm:text-left">
                  <div className="flex items-center justify-center gap-2.5 sm:justify-start">
                    <Avatar
                      url={match.awayTeam?.logoPath}
                      shape="square"
                      size="md"
                      fallback={teamInitials(match.awayTeam?.name, match.awayTeam?.tag)}
                      alt=""
                    />
                    <div className="text-lead font-medium text-text">{match.awayTeam?.name ?? "Очікується"}</div>
                  </div>
                  <div className="flex items-center justify-center gap-2 sm:justify-start">
                    <span className="font-mono text-micro text-text-faint">{match.awayTeam?.tag}</span>
                    <RatingDelta delta={ratingDelta?.awayDelta} />
                  </div>
                </div>
              </div>

              {canScore && isLive && (
                <div className="mt-6 grid gap-3 border-t border-line-soft pt-5 sm:grid-cols-2">
                  {[
                    { label: match.homeTeam?.name ?? "Господарі", home: true },
                    { label: match.awayTeam?.name ?? "Гості", home: false }
                  ].map((side) => (
                    <div key={side.label} className="flex items-center justify-between gap-3">
                      <span className="truncate text-body text-text-muted">{side.label}</span>
                      <div className="flex items-center gap-2">
                        <button
                          type="button"
                          disabled={busy}
                          onClick={() => adjustScore(side.home ? -1 : 0, side.home ? 0 : -1)}
                          className="btn btn-secondary btn-sm px-2"
                          aria-label={`Зменшити рахунок: ${side.label}`}
                        >
                          <Minus className="h-4 w-4" />
                        </button>
                        <button
                          type="button"
                          disabled={busy}
                          onClick={() => adjustScore(side.home ? 1 : 0, side.home ? 0 : 1)}
                          className="btn btn-primary btn-sm px-2"
                          aria-label={`Збільшити рахунок: ${side.label}`}
                        >
                          <Plus className="h-4 w-4" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {canScore && isLive && (
                <div className="mt-6 space-y-4 border-t border-line-soft pt-5">
                  <div className="eyebrow">Завершення матчу</div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <label className="field">
                      Переможець
                      <select
                        value={winnerChoice}
                        onChange={(event) => setWinnerChoice(event.target.value)}
                        className="input"
                      >
                        <option value="">Нічия</option>
                        {match.homeTeam && <option value={match.homeTeam.id}>{match.homeTeam.name}</option>}
                        {match.awayTeam && <option value={match.awayTeam.id}>{match.awayTeam.name}</option>}
                      </select>
                      {match.round > 0 && (
                        <p className="field-hint">Матч сітки не може завершитися внічию.</p>
                      )}
                    </label>

                    <label className="field">
                      Матч у трекері статистики
                      <input
                        type="url"
                        value={finishTracker}
                        onChange={(event) => setFinishTracker(event.target.value)}
                        placeholder="https://tracker.gg/valorant/match/..."
                        className="input"
                      />
                      <p className="field-hint">Необовʼязково — можна додати й пізніше.</p>
                    </label>
                  </div>

                  <button
                    type="button"
                    disabled={busy}
                    onClick={() =>
                      run(async () => {
                        await matchesApi.complete(matchId, {
                          winnerTeamId: winnerChoice ? Number(winnerChoice) : null,
                          trackerUrl: finishTracker.trim() || null
                        });
                        setFinishTracker("");
                        await load();
                      })
                    }
                    className="btn btn-primary"
                  >
                    Завершити матч
                  </button>
                </div>
              )}

              {canScore && match.status === "Scheduled" && (
                <div className="mt-6 border-t border-line-soft pt-5">
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => run(async () => { await matchesApi.start(matchId); await load(); })}
                    className="btn btn-primary"
                  >
                    Розпочати матч
                  </button>
                </div>
              )}
            </div>
          </section>

          {canScore && (
            <section className="panel">
              <div className="panel-header">
                <h2 className="section-title">Посилання</h2>
              </div>
              <div className="panel-body space-y-4">
                <label className="field">
                  Трансляція
                  <input
                    type="url"
                    value={streamDraft}
                    onChange={(event) => {
                      setStreamDraft(event.target.value);
                      setLinksSaved(false);
                    }}
                    placeholder="https://twitch.tv/..."
                    className="input"
                  />
                  <p className="field-hint">Twitch або YouTube, через https://.</p>
                </label>

                <label className="field">
                  Матч у трекері статистики
                  <input
                    type="url"
                    value={trackerDraft}
                    onChange={(event) => {
                      setTrackerDraft(event.target.value);
                      setLinksSaved(false);
                    }}
                    placeholder="https://tracker.gg/valorant/match/..."
                    className="input"
                  />
                  <p className="field-hint">
                    Необовʼязково. Будь-яке https-посилання: tracker.gg, HLTV, Dotabuff, OP.GG.
                  </p>
                </label>

                <div className="flex items-center gap-3 border-t border-line-soft pt-4">
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() =>
                      run(async () => {
                        const updated = await matchesApi.updateLinks(matchId, {
                          streamUrl: streamDraft.trim() || null,
                          trackerUrl: trackerDraft.trim() || null
                        });
                        setMatch((prev) =>
                          prev
                            ? { ...prev, streamUrl: updated.streamUrl, trackerUrl: updated.trackerUrl }
                            : prev
                        );
                        setStreamDraft(updated.streamUrl ?? "");
                        setTrackerDraft(updated.trackerUrl ?? "");
                        setLinksSaved(true);
                      })
                    }
                    className="btn btn-primary btn-sm"
                  >
                    Зберегти посилання
                  </button>
                  {linksSaved && <span className="text-micro text-win">Збережено.</span>}
                </div>
              </div>
            </section>
          )}

          <section className="panel">
            <div className="panel-header">
              <h2 className="section-title">Ростер і статистика</h2>
              {canScore && (
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => run(async () => setRoster(await matchesApi.autofillRoster(matchId)))}
                  className="btn btn-secondary btn-sm"
                >
                  <UserPlus className="h-4 w-4" />
                  Заповнити зі складів
                </button>
              )}
            </div>
            <div className="panel-body">
              {roster.length === 0 ? (
                <EmptyState
                  title="Ростер порожній"
                  hint={
                    canScore
                      ? "Заповніть ростер активними гравцями обох команд — далі можна вносити вбивства, смерті та асисти."
                      : "Організатор ще не вніс склади на цей матч."
                  }
                />
              ) : (
                <div className="space-y-7">
                  <RosterTable title={match.homeTeam?.name ?? "Господарі"} entries={homeRoster} />
                  <RosterTable title={match.awayTeam?.name ?? "Гості"} entries={awayRoster} />
                </div>
              )}
            </div>
          </section>
        </>
      )}
    </>
  );
};

export default MatchDetail;
