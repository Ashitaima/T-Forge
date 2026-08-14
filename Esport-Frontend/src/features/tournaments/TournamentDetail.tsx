import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { AlertCircle, CheckCircle2, Users } from "lucide-react";
import { matchesApi } from "../../api/matchesApi";
import { teamsApi } from "../../api/teamsApi";
import { tournamentsApi } from "../../api/tournamentsApi";
import { useAuthStore } from "../../store/authStore";
import { EmptyState, PageHeader, Skeleton, StatusPill } from "../../components/ui/Primitives";
import { BracketView } from "./BracketView";
import type { MatchDto, TeamDto, TeamSummaryDto, TournamentDto } from "../../types";

const TournamentDetail = () => {
  const { id } = useParams();
  const tournamentId = Number(id);
  const { user } = useAuthStore();

  const [tournament, setTournament] = useState<TournamentDto | null>(null);
  const [matches, setMatches] = useState<MatchDto[]>([]);
  const [registered, setRegistered] = useState<TeamSummaryDto[]>([]);
  const [allTeams, setAllTeams] = useState<TeamDto[]>([]);
  const [selectedTeam, setSelectedTeam] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const canManage =
    user?.role === "Admin" || (user?.role === "Organizer" && tournament?.organizer?.id === user?.id);
  const isRegistrationOpen = tournament?.status === "Registration";

  const load = useCallback(async () => {
    if (!tournamentId) {
      return;
    }

    setLoading(true);
    try {
      const [tournamentData, matchesData, registeredData, teamsData] = await Promise.all([
        tournamentsApi.getById(tournamentId),
        matchesApi.getPaged({ page: 1, pageSize: 64, tournamentId }),
        tournamentsApi.getRegisteredTeams(tournamentId).catch(() => [] as TeamSummaryDto[]),
        teamsApi.getPaged({ page: 1, pageSize: 100 }).then((r) => r.data).catch(() => [] as TeamDto[])
      ]);
      setTournament(tournamentData);
      setMatches(matchesData.data);
      setRegistered(registeredData);
      setAllTeams(teamsData);
    } finally {
      setLoading(false);
    }
  }, [tournamentId]);

  useEffect(() => {
    load();
  }, [load]);

  // Бекенд повертає зрозумілі повідомлення про помилки — показуємо їх як є
  const run = async (action: () => Promise<unknown>, successMessage: string) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await action();
      setNotice(successMessage);
      await load();
    } catch (err: unknown) {
      const response = (err as { response?: { data?: { message?: string } } }).response;
      setError(response?.data?.message ?? "Не вдалося виконати дію");
    } finally {
      setBusy(false);
    }
  };

  const availableTeams = useMemo(
    () => allTeams.filter((team) => !registered.some((r) => r.id === team.id)),
    [allTeams, registered]
  );

  const hasBracket = matches.some((match) => match.round > 0);
  const slotsLeft = tournament ? tournament.maxTeams - registered.length : 0;

  return (
    <>
      <PageHeader
        eyebrow={tournament?.game}
        title={tournament?.name ?? `Турнір #${id}`}
        description={tournament?.description}
        action={
          canManage ? (
            <Link to={`/tournaments/${id}/edit`} className="btn btn-secondary">
              Редагувати
            </Link>
          ) : undefined
        }
      />

      {tournament && (
        <div className="flex flex-wrap items-center gap-x-6 gap-y-3">
          <StatusPill status={tournament.status} />
          <span className="flex items-center gap-2 text-body text-text-muted">
            <Users className="h-4 w-4 text-text-faint" />
            <span className="tabular font-mono text-text">
              {registered.length}/{tournament.maxTeams}
            </span>
            команд
          </span>
          <span className="text-body text-text-muted">
            Призовий фонд{" "}
            <span className="tabular font-mono text-text">${tournament.prizePool}</span>
          </span>
          <span className="tabular font-mono text-micro text-text-faint">
            {new Date(tournament.startDate).toLocaleDateString("uk-UA")} —{" "}
            {new Date(tournament.endDate).toLocaleDateString("uk-UA")}
          </span>
        </div>
      )}

      {error && (
        <div className="notice notice-error">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}
      {notice && (
        <div className="notice notice-success">
          <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" />
          <span>{notice}</span>
        </div>
      )}

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Турнірна сітка</h2>
          {canManage && isRegistrationOpen && (
            <button
              type="button"
              disabled={busy || registered.length < 2}
              onClick={() => run(() => tournamentsApi.generateBracket(tournamentId), "Турнірну сітку створено")}
              className="btn btn-primary"
            >
              Згенерувати сітку
            </button>
          )}
        </div>
        <div className="panel-body">
          {loading && <Skeleton rows={2} />}
          {!loading && !hasBracket && (
            <EmptyState
              title="Сітку ще не створено"
              hint={
                canManage
                  ? "Зареєструйте щонайменше дві команди, потім згенеруйте сітку — вона розставить пари й розклад автоматично."
                  : "Організатор згенерує сітку, коли реєстрація завершиться."
              }
            />
          )}
          {!loading && hasBracket && <BracketView matches={matches} />}
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Учасники</h2>
          {isRegistrationOpen && (
            <span className="font-mono text-micro text-text-faint">
              {slotsLeft > 0 ? `вільних місць: ${slotsLeft}` : "місць немає"}
            </span>
          )}
        </div>

        {isRegistrationOpen && (
          <div className="flex flex-wrap gap-3 border-b border-line-soft px-5 py-4">
            <label className="sr-only" htmlFor="team-picker">
              Команда для реєстрації
            </label>
            <select
              id="team-picker"
              value={selectedTeam}
              onChange={(event) => setSelectedTeam(event.target.value)}
              className="input min-w-[15rem] flex-1"
            >
              <option value="">Оберіть команду</option>
              {availableTeams.map((team) => (
                <option key={team.id} value={team.id}>
                  {team.name} ({team.tag})
                </option>
              ))}
            </select>
            <button
              type="button"
              disabled={!selectedTeam || busy || slotsLeft <= 0}
              onClick={() =>
                run(
                  () => tournamentsApi.registerTeam(tournamentId, Number(selectedTeam)),
                  "Команду зареєстровано"
                ).then(() => setSelectedTeam(""))
              }
              className="btn btn-primary"
            >
              Зареєструвати
            </button>
          </div>
        )}

        <div className="panel-body">
          {loading && <Skeleton rows={2} />}
          {!loading && registered.length === 0 && (
            <EmptyState
              title="Ще немає заявок"
              hint="Капітан команди або організатор може зареєструвати склад, поки триває реєстрація."
            />
          )}
          {!loading && registered.length > 0 && (
            <ol className="divide-y divide-line-soft">
              {registered.map((team, index) => (
                <li key={team.id} className="flex items-center gap-4 py-3 first:pt-0 last:pb-0">
                  {/* Номер посіву — реальна інформація: він визначає пари у сітці */}
                  <span className="tabular w-6 shrink-0 font-mono text-micro text-text-faint">
                    {String(index + 1).padStart(2, "0")}
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className="truncate text-body font-medium text-text">{team.name}</div>
                    <div className="font-mono text-micro text-text-faint">
                      {team.tag}
                      {team.region ? ` · ${team.region}` : ""}
                    </div>
                  </div>
                  {isRegistrationOpen && (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => run(() => tournamentsApi.withdrawTeam(tournamentId, team.id), "Команду знято")}
                      className="btn btn-ghost btn-sm"
                    >
                      Зняти
                    </button>
                  )}
                </li>
              ))}
            </ol>
          )}
        </div>
      </section>
    </>
  );
};

export default TournamentDetail;
