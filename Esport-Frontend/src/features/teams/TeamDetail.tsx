import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { AlertCircle } from "lucide-react";
import { matchesApi } from "../../api/matchesApi";
import { membershipRequestsApi } from "../../api/membershipRequestsApi";
import { playersApi } from "../../api/playersApi";
import { ratingsApi } from "../../api/ratingsApi";
import { teamsApi } from "../../api/teamsApi";
import { useIsCaptainOf, useIsRole } from "../../hooks/useEffectiveRole";
import { EmptyState, PageHeader, Pager, Skeleton } from "../../components/ui/Primitives";
import { CountryFlag } from "../../components/ui/CountryFlag";
import { Avatar, teamInitials } from "../../components/ui/Avatar";
import { RatingPanel } from "../../components/ui/Rating";
import { TournamentInvitationsPanel } from "../tournaments/TournamentInvitationsPanel";
import { TeamChallengesPanel } from "./TeamChallengesPanel";
import { regionLabel } from "../../constants/regions";
import { usePagedList } from "../../hooks/usePagedList";
import { MatchResultBadge } from "../matches/MatchResultBadge";
import type { MatchDto, MembershipRequestDto, PlayerDto,
  PlayerRowDto, RatingChangeDto, RatingDto, TeamDto, TeamSummaryStatsDto } from "../../types";

const HISTORY_PAGE_SIZE = 10;

const TeamDetail = () => {
  const { id } = useParams();
  const teamId = Number(id);

  const [team, setTeam] = useState<TeamDto | null>(null);
  const [summary, setSummary] = useState<TeamSummaryStatsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [missing, setMissing] = useState(false);

  // Профіль і підсумок не залежать від сторінки історії
  const teamRequestToken = useRef(0);

  const loadTeam = useCallback(() => {
    if (Number.isNaN(teamId)) {
      setMissing(true);
      setLoading(false);
      return;
    }

    const token = ++teamRequestToken.current;
    setLoading(true);

    Promise.all([
      teamsApi.getWithPlayers(teamId),
      teamsApi.getSummary(teamId).catch(() => null)
    ])
      .then(([teamData, summaryData]) => {
        if (token !== teamRequestToken.current) {
          return; // застаріла відповідь — команду вже змінили
        }
        setTeam(teamData);
        setSummary(summaryData);
      })
      .catch(() => {
        if (token === teamRequestToken.current) {
          setMissing(true);
        }
      })
      .finally(() => {
        if (token === teamRequestToken.current) {
          setLoading(false);
        }
      });
  }, [teamId]);

  useEffect(() => {
    loadTeam();
  }, [loadTeam]);

  // Рейтинг і його історія — окремі запити: вони не залежать ані від складу,
  // ані від сторінки історії матчів.
  const [ratings, setRatings] = useState<RatingDto[]>([]);
  const [ratingHistory, setRatingHistory] = useState<RatingChangeDto[]>([]);

  useEffect(() => {
    if (Number.isNaN(teamId)) {
      return;
    }

    let isActive = true;

    Promise.all([
      ratingsApi.getTeamRatings(teamId).catch(() => [] as RatingDto[]),
      ratingsApi.getTeamHistory(teamId).catch(() => [] as RatingChangeDto[])
    ]).then(([ratingRows, historyRows]) => {
      if (isActive) {
        setRatings(ratingRows);
        setRatingHistory(historyRows);
      }
    });

    return () => {
      isActive = false;
    };
  }, [teamId]);

  const {
    items: matches,
    page,
    setPage,
    totalCount,
    totalPages,
    loading: historyLoading
  } = usePagedList<MatchDto>(
    (page, pageSize) =>
      matchesApi.getPaged({ page, pageSize, teamId, sortBy: "scheduledAt", sortDirection: "desc" }),
    String(teamId),
    HISTORY_PAGE_SIZE
  );

  const [myPlayer, setMyPlayer] = useState<PlayerDto | null>(null);
  const [requests, setRequests] = useState<MembershipRequestDto[]>([]);

  // Капітанство читаємо через хук: у режимі розробника адміністратор може
  // подивитися на сторінку очима капітана саме цієї команди.
  const isCaptain = useIsCaptainOf(teamId, team?.captain?.id);
  const isAdmin = useIsRole("Admin");

  // Логотип змінює капітан, а адміністратор — як і всюди — стоїть над правилом.
  const canManageLogo = isCaptain || isAdmin;

  const [logoBusy, setLogoBusy] = useState(false);
  const [logoError, setLogoError] = useState<string | null>(null);

  // Ті самі межі, що на сервері (Common/AvatarRules.cs) — щоб про завеликий
  // файл користувач дізнався ще до вивантаження.
  const MAX_LOGO_BYTES = 2 * 1024 * 1024;
  const ALLOWED_LOGO_TYPES = ["image/jpeg", "image/png", "image/webp"];

  const readLogoError = (caught: unknown, fallback: string) =>
    (caught as { response?: { data?: { message?: string } } })?.response?.data?.message ?? fallback;

  const uploadLogo = async (file: File) => {
    setLogoError(null);
    if (!ALLOWED_LOGO_TYPES.includes(file.type)) {
      setLogoError("Підтримуються лише JPEG, PNG і WebP.");
      return;
    }
    if (file.size > MAX_LOGO_BYTES) {
      setLogoError("Файл завеликий: максимум 2 МБ.");
      return;
    }

    setLogoBusy(true);
    try {
      setTeam(await teamsApi.uploadLogo(teamId, file));
    } catch (caught) {
      setLogoError(readLogoError(caught, "Не вдалося завантажити логотип."));
    } finally {
      setLogoBusy(false);
    }
  };

  const removeLogo = async () => {
    setLogoBusy(true);
    setLogoError(null);
    try {
      await teamsApi.deleteLogo(teamId);
      setTeam((prev) => (prev ? { ...prev, logoPath: null } : prev));
    } catch (caught) {
      setLogoError(readLogoError(caught, "Не вдалося прибрати логотип."));
    } finally {
      setLogoBusy(false);
    }
  };

  useEffect(() => {
    playersApi
      .getMe()
      .then(setMyPlayer)
      .catch(() => setMyPlayer(null)); // користувач без профілю гравця — це нормально
  }, []);

  const requestsToken = useRef(0);

  const loadRequests = useCallback(() => {
    if (Number.isNaN(teamId) || !(isCaptain || isAdmin)) {
      setRequests([]);
      return;
    }

    const token = ++requestsToken.current;

    membershipRequestsApi
      .getForTeam(teamId, "Pending")
      .then((data) => {
        if (token === requestsToken.current) {
          setRequests(data);
        }
      })
      .catch(() => {
        if (token === requestsToken.current) {
          setRequests([]);
        }
      });
  }, [teamId, isCaptain, isAdmin]);

  useEffect(() => {
    loadRequests();
  }, [loadRequests]);

  const applications = requests.filter((r) => r.direction === "Application");
  const invitations = requests.filter((r) => r.direction === "Invite");

  const [actionError, setActionError] = useState<string | null>(null);

  // Бекенд повертає зрозумілі повідомлення про помилки — показуємо їх як є
  const runAction = async (action: () => Promise<unknown>) => {
    setActionError(null);
    try {
      await action();
    } catch (err: unknown) {
      const response = (err as { response?: { data?: { message?: string } } }).response;
      setActionError(response?.data?.message ?? "Не вдалося виконати дію");
    }
  };

  const respond = (action: "accept" | "decline" | "cancel", requestId: number) =>
    runAction(async () => {
      await membershipRequestsApi[action](requestId);
      loadRequests();
      loadTeam(); // прийнята заявка змінює склад — перечитуємо команду
    });

  const [freeAgentSearch, setFreeAgentSearch] = useState("");
  const [freeAgents, setFreeAgents] = useState<PlayerRowDto[]>([]);

  useEffect(() => {
    if (!(isCaptain || isAdmin)) {
      return;
    }

    playersApi
      .getPaged({ freeAgents: true, search: freeAgentSearch, page: 1, pageSize: 10 })
      .then((response) => setFreeAgents(response.data))
      .catch(() => setFreeAgents([]));
  }, [freeAgentSearch, isCaptain, isAdmin]);

  const invite = (playerId: number) =>
    runAction(async () => {
      await membershipRequestsApi.invite(teamId, playerId);
      loadRequests();
    });

  // Передача капітанства забирає право в того, хто її робить, тож питаємо
  // підтвердження: скасувати дію може вже тільки новий капітан або адміністратор.
  const [transferring, setTransferring] = useState<number | null>(null);

  const transferCaptaincy = (playerId: number) =>
    runAction(async () => {
      await teamsApi.transferCaptaincy(teamId, playerId);
      setTransferring(null);
      loadTeam();
    });

  const canApply =
    Boolean(myPlayer) && !myPlayer?.team && !isCaptain && Boolean(team?.isActive);

  // Заявка гравця на цю команду і запрошення від неї — різні напрямки,
  // їх не можна плутати: скасувати можна лише те, що ти сам ініціював.
  const [myPendingForTeam, setMyPendingForTeam] = useState<MembershipRequestDto | null>(null);

  useEffect(() => {
    if (!myPlayer) {
      setMyPendingForTeam(null);
      return;
    }

    membershipRequestsApi
      .getForPlayer(myPlayer.id, "Pending")
      .then((rows) => setMyPendingForTeam(rows.find((r) => r.teamId === teamId) ?? null))
      .catch(() => setMyPendingForTeam(null));
  }, [myPlayer, teamId]);

  const myPendingApplication =
    myPendingForTeam?.direction === "Application" ? myPendingForTeam : null;
  const myPendingInvitation = myPendingForTeam?.direction === "Invite" ? myPendingForTeam : null;

  const apply = () => {
    if (!myPlayer) {
      return Promise.resolve();
    }

    return runAction(async () => {
      const created = await membershipRequestsApi.apply(myPlayer.id, teamId);
      setMyPendingForTeam(created);
    });
  };

  if (missing) {
    return (
      <EmptyState
        title="Команду не знайдено"
        hint="Можливо, її було видалено."
        action={
          <Link to="/teams" className="btn btn-primary">
            До списку команд
          </Link>
        }
      />
    );
  }

  const resultFor = (match: MatchDto) => {
    if (match.status !== "Completed" || !match.winnerTeam) {
      return "Pending";
    }
    return match.winnerTeam.id === teamId ? "Win" : "Loss";
  };

  return (
    <>
      {/* Логотип стоїть поруч із заголовком, а не всередині нього: PageHeader
          спільний для всіх сторінок і приймає лише рядок. */}
      <div className="flex items-center gap-4">
        <Avatar
          url={team?.logoPath}
          shape="square"
          size="lg"
          fallback={teamInitials(team?.name, team?.tag)}
          alt={`Логотип ${team?.name ?? "команди"}`}
        />
        <div className="min-w-0 flex-1">
      <PageHeader
        eyebrow={team?.tag}
        title={team?.name ?? `Команда #${id}`}
        description={team?.description}
        action={
          isCaptain ? (
            <Link to={`/teams/${id}/edit`} className="btn btn-secondary">
              Редагувати
            </Link>
          ) : myPendingApplication ? (
            <div className="flex items-center gap-3">
              <span className="pill">Заявку надіслано</span>
              <button
                className="btn btn-ghost btn-sm"
                onClick={() =>
                  runAction(async () => {
                    await membershipRequestsApi.cancel(myPendingApplication.id);
                    setMyPendingForTeam(null);
                  })
                }
              >
                Скасувати
              </button>
            </div>
          ) : myPendingInvitation ? (
            <Link to={`/players/${myPlayer?.id}`} className="pill hover:text-ember">
              Вас запрошено
            </Link>
          ) : canApply ? (
            <button className="btn btn-primary" onClick={apply}>
              Подати заявку
            </button>
          ) : undefined
        }
      />
        </div>
      </div>

      {canManageLogo && (
        <section className="panel">
          <div className="panel-header">
            <h2 className="section-title">Логотип</h2>
          </div>
          <div className="panel-body space-y-4">
            <div className="flex flex-wrap items-center gap-5">
              <Avatar
                url={team?.logoPath}
                shape="square"
                size="lg"
                fallback={teamInitials(team?.name, team?.tag)}
                alt=""
              />
              <div className="space-y-2">
                <label className="btn btn-secondary btn-sm cursor-pointer">
                  {logoBusy ? "Завантаження..." : "Вибрати файл"}
                  <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    className="hidden"
                    disabled={logoBusy}
                    onChange={(event) => {
                      const file = event.target.files?.[0];
                      if (file) {
                        uploadLogo(file);
                      }
                      // Скидаємо значення, щоб повторний вибір того самого файлу
                      // теж викликав onChange.
                      event.target.value = "";
                    }}
                  />
                </label>
                {team?.logoPath && (
                  <button
                    type="button"
                    onClick={removeLogo}
                    disabled={logoBusy}
                    className="btn btn-ghost btn-sm"
                  >
                    Прибрати
                  </button>
                )}
                <p className="field-hint">JPEG, PNG або WebP, до 2 МБ.</p>
              </div>
            </div>
            {logoError && <div className="notice notice-error">{logoError}</div>}
          </div>
        </section>
      )}

      {actionError && (
        <div className="notice notice-error">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          <span>{actionError}</span>
        </div>
      )}

      <p className="text-body text-text-muted">
        Капітан{" "}
        <span className="text-text">
          {team?.captain ? `@${team.captain.username}` : "не призначено"}
        </span>
        {team?.region && (
          <>
            {" "}
            · Регіон <span className="text-text">{regionLabel(team.region)}</span>
          </>
        )}
        {" "}
        · Склад <span className="tabular font-mono text-text">{team?.players?.length ?? 0}</span>
      </p>

      {summary && (
        <div className="surface-raised flex flex-wrap items-center gap-x-8 gap-y-3 px-5 py-4">
          <span className="text-body text-text-muted">
            Ігор <span className="tabular font-mono text-text">{summary.played}</span>
          </span>
          <span className="text-body text-text-muted">
            Перемог <span className="tabular font-mono text-win">{summary.wins}</span>
          </span>
          <span className="text-body text-text-muted">
            Поразок <span className="tabular font-mono text-text">{summary.losses}</span>
          </span>
          <span className="text-body text-text-muted">
            Відсоток <span className="tabular font-mono text-text">{summary.winRate}%</span>
          </span>
          {summary.streak && (
            <span className="text-body text-text-muted">
              Серія{" "}
              <span
                className={`tabular font-mono ${summary.streak.type === "Win" ? "text-win" : "text-danger"}`}
              >
                {summary.streak.count} {summary.streak.type === "Win" ? "П" : "Пр"}
              </span>
            </span>
          )}
        </div>
      )}

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Рейтинг</h2>
        </div>
        <div className="panel-body">
          <RatingPanel
            ratings={ratings}
            history={ratingHistory}
            emptyHint="Він зʼявиться після першого зіграного турнірного матчу — практичні матчі рейтинг не змінюють."
          />
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Історія матчів</h2>
          <span className="tabular font-mono text-micro text-text-faint">{totalCount}</span>
        </div>
        <div className="panel-body space-y-4">
          {historyLoading && <Skeleton rows={3} />}
          {!historyLoading && matches.length === 0 && (
            <EmptyState
              title="Матчів ще не було"
              hint="Історія зʼявиться після першого зіграного матчу команди."
            />
          )}
          {!historyLoading && matches.length > 0 && (
            <ul className="divide-y divide-line-soft">
              {matches.map((match) => {
                const isHome = match.homeTeam?.id === teamId;
                const opponent = isHome ? match.awayTeam : match.homeTeam;
                const own = isHome ? match.homeTeamScore : match.awayTeamScore;
                const other = isHome ? match.awayTeamScore : match.homeTeamScore;

                return (
                  <li key={match.id} className="flex flex-wrap items-center gap-x-5 gap-y-2 py-3">
                    <MatchResultBadge result={resultFor(match)} />
                    <span className="tabular w-20 font-mono text-micro text-text-faint">
                      {new Date(match.scheduledAt).toLocaleDateString("uk-UA", {
                        day: "2-digit",
                        month: "short"
                      })}
                    </span>
                    <Link
                      to={`/matches/${match.id}`}
                      className="min-w-0 flex-1 truncate text-body text-text hover:text-ember"
                    >
                      {opponent?.name ?? "Очікується"}
                    </Link>
                    <span className="tabular font-mono text-body text-text">
                      {own}:{other}
                    </span>
                    <span className="w-full text-micro text-text-faint sm:w-auto">
                      {match.tournament?.name ?? "—"} · {match.matchType}
                    </span>
                  </li>
                );
              })}
            </ul>
          )}
          <Pager
            page={page}
            pageSize={HISTORY_PAGE_SIZE}
            totalCount={totalCount}
            totalPages={totalPages}
            onChange={setPage}
            disabled={historyLoading}
          />
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Склад</h2>
        </div>
        <div className="panel-body">
          {loading && <Skeleton rows={3} />}
          {!loading && (team?.players?.length ?? 0) === 0 && (
            <EmptyState
              title="Склад порожній"
              hint="Гравці зʼявляться тут, щойно капітан надішле запрошення або схвалить заявку."
            />
          )}
          {!loading && (team?.players?.length ?? 0) > 0 && (
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              {team?.players?.map((player) => {
                const isTeamCaptain =
                  player.userId > 0 && player.userId === team?.captain?.id;

                return (
                  <div key={player.id} className="surface-raised px-4 py-3.5">
                    <div className="flex items-start justify-between gap-2">
                      <span className="truncate text-body font-medium text-text">
                        {player.nickname}
                      </span>
                      <div className="flex shrink-0 items-center gap-2">
                        {isTeamCaptain && <span className="pill">Капітан</span>}
                        {!player.isActive && <span className="pill pill-off">Неактивний</span>}
                      </div>
                    </div>
                    <div className="muted mt-1 text-micro">
                      {player.position || "Позиція не вказана"}
                    </div>
                    {player.country && (
                      <div className="mt-2 text-micro text-text-faint">
                        <CountryFlag code={player.country} withName />
                      </div>
                    )}

                    {/* Гравець без акаунта капітаном стати не може: Team.CaptainId
                        посилається на користувача, а не на гравця. */}
                    {(isCaptain || isAdmin) && !isTeamCaptain && player.userId > 0 && (
                      <div className="mt-3 border-t border-line-soft pt-3">
                        {transferring === player.id ? (
                          <div className="flex flex-wrap items-center gap-2">
                            <span className="text-micro text-text-muted">
                              {isCaptain && !isAdmin
                                ? "Ви втратите права капітана."
                                : "Змінити капітана команди?"}
                            </span>
                            <button
                              type="button"
                              className="btn btn-primary btn-sm"
                              onClick={() => transferCaptaincy(player.id)}
                            >
                              Підтвердити
                            </button>
                            <button
                              type="button"
                              className="btn btn-ghost btn-sm"
                              onClick={() => setTransferring(null)}
                            >
                              Скасувати
                            </button>
                          </div>
                        ) : (
                          <button
                            type="button"
                            className="btn btn-ghost btn-sm"
                            onClick={() => setTransferring(player.id)}
                          >
                            Зробити капітаном
                          </button>
                        )}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </section>

      {(isCaptain || isAdmin) && (
        <section className="panel panel-body mt-6">
          <div className="eyebrow">Заявки на вступ</div>

          {applications.length === 0 && (
            <p className="muted mt-3 text-micro">Немає нових заявок.</p>
          )}

          {applications.map((request) => (
            <div
              key={request.id}
              className="surface-raised mt-3 flex items-center justify-between px-4 py-3.5"
            >
              <div className="min-w-0">
                <div className="truncate text-body font-medium text-text">
                  {request.playerNickname}
                </div>
                <div className="muted mt-1 text-micro">
                  {request.playerPosition || "Позиція не вказана"}
                </div>
              </div>
              <div className="flex shrink-0 gap-2">
                <button className="btn btn-primary btn-sm" onClick={() => respond("accept", request.id)}>
                  Прийняти
                </button>
                <button className="btn btn-ghost btn-sm" onClick={() => respond("decline", request.id)}>
                  Відхилити
                </button>
              </div>
            </div>
          ))}
        </section>
      )}

      {(isCaptain || isAdmin) && invitations.length > 0 && (
        <section className="panel panel-body mt-6">
          <div className="eyebrow">Надіслані запрошення</div>

          {invitations.map((request) => (
            <div
              key={request.id}
              className="surface-raised mt-3 flex items-center justify-between px-4 py-3.5"
            >
              <div className="min-w-0">
                <div className="truncate text-body font-medium text-text">
                  {request.playerNickname}
                </div>
                <span className="pill mt-1">Очікує відповіді</span>
              </div>
              <button className="btn btn-ghost btn-sm" onClick={() => respond("cancel", request.id)}>
                Скасувати
              </button>
            </div>
          ))}
        </section>
      )}

      {(isCaptain || isAdmin) && (
        <section className="panel panel-body mt-6">
          <div className="eyebrow">Запросити гравця</div>

          <input
            className="input mt-3 w-full"
            placeholder="Пошук вільних гравців"
            value={freeAgentSearch}
            onChange={(event) => setFreeAgentSearch(event.target.value)}
          />

          {freeAgents.length === 0 && (
            <p className="muted mt-3 text-micro">Вільних гравців не знайдено.</p>
          )}

          {freeAgents.map((player) => (
            <div
              key={player.id}
              className="surface-raised mt-3 flex items-center justify-between px-4 py-3.5"
            >
              <span className="truncate text-body text-text">{player.nickname}</span>
              <button className="btn btn-secondary btn-sm" onClick={() => invite(player.id)}>
                Запросити
              </button>
            </div>
          ))}
        </section>
      )}

      {team && <TournamentInvitationsPanel teamId={team.id} isCaptain={isCaptain || isAdmin} />}

      {team && (
        <TeamChallengesPanel teamId={team.id} teamCaptainId={team.captain?.id ?? null} />
      )}

    </>
  );
};

export default TeamDetail;
