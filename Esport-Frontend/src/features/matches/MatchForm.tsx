import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { matchesApi } from "../../api/matchesApi";
import { teamsApi } from "../../api/teamsApi";
import { tournamentsApi } from "../../api/tournamentsApi";
import type { CreateMatchDto, MatchDto, TeamDto, TournamentDto, UpdateMatchDto } from "../../types";

const schema = z.object({
  tournamentId: z.coerce.number().optional(),
  homeTeamId: z.coerce.number().optional(),
  awayTeamId: z.coerce.number().optional(),
  scheduledAt: z.string().min(1, "Вкажіть час"),
  matchType: z.string().optional(),
  format: z.string().optional(),
  notes: z.string().optional(),
  status: z.string().optional(),
  homeTeamScore: z.coerce.number().optional(),
  awayTeamScore: z.coerce.number().optional(),
  winnerTeamId: z.coerce.number().optional(),
  startedAt: z.string().optional(),
  endedAt: z.string().optional()
});

type FormValues = z.infer<typeof schema>;

const MatchForm = () => {
  const navigate = useNavigate();
  const { id } = useParams();
  const [loading, setLoading] = useState(false);
  const [tournaments, setTournaments] = useState<TournamentDto[]>([]);
  const [teams, setTeams] = useState<TeamDto[]>([]);
  const [match, setMatch] = useState<MatchDto | null>(null);
  const {
    register,
    handleSubmit,
    setValue,
    setError,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  // Турніри й команди підвантажуються списками, щоб не вводити ID вручну
  useEffect(() => {
    let isActive = true;

    Promise.all([
      tournamentsApi.getAllActive().catch(() => [] as TournamentDto[]),
      teamsApi.getPaged({ page: 1, pageSize: 100 }).then((r) => r.data).catch(() => [] as TeamDto[])
    ]).then(([tournamentList, teamList]) => {
      if (isActive) {
        setTournaments(tournamentList);
        setTeams(teamList);
      }
    });

    return () => {
      isActive = false;
    };
  }, []);

  useEffect(() => {
    if (!id) {
      return;
    }

    const load = async () => {
      setLoading(true);
      try {
        const data = await matchesApi.getById(Number(id));
        setMatch(data);
        setValue("scheduledAt", data.scheduledAt.slice(0, 16));
        setValue("status", data.status);
        setValue("homeTeamScore", data.homeTeamScore);
        setValue("awayTeamScore", data.awayTeamScore);
        setValue("winnerTeamId", data.winnerTeam?.id ?? undefined);
        setValue("notes", data.notes);
        setValue("startedAt", data.startedAt?.slice(0, 16));
        setValue("endedAt", data.endedAt?.slice(0, 16));
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [id, setValue]);

  const onSubmit = async (values: FormValues) => {
    if (!id) {
      if (!values.tournamentId || !values.homeTeamId || !values.awayTeamId) {
        setError("tournamentId", { message: "Вкажіть турнір та команди" });
        return;
      }
      const payload: CreateMatchDto = {
        tournamentId: values.tournamentId,
        homeTeamId: values.homeTeamId,
        awayTeamId: values.awayTeamId,
        scheduledAt: values.scheduledAt,
        matchType: values.matchType ?? "GroupStage",
        format: values.format ?? "BO1",
        notes: values.notes ?? ""
      };
      await matchesApi.create(payload);
    } else {
      const payload: UpdateMatchDto = {
        scheduledAt: values.scheduledAt,
        status: values.status ?? "Scheduled",
        homeTeamScore: values.homeTeamScore ?? 0,
        awayTeamScore: values.awayTeamScore ?? 0,
        winnerTeamId: values.winnerTeamId ? Number(values.winnerTeamId) : null,
        notes: values.notes ?? "",
        startedAt: values.startedAt ?? null,
        endedAt: values.endedAt ?? null
      };
      await matchesApi.update(Number(id), payload);
    }

    navigate("/matches");
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <header className="border-b border-line-soft pb-5">
        <h1 className="page-title">{id ? "Редагування матчу" : "Новий матч"}</h1>
        <p className="muted mt-2 text-body">Контролюйте розклад та результати матчів.</p>
      </header>
      <form onSubmit={handleSubmit(onSubmit)} className="panel panel-body space-y-5">
        {!id && (
          <>
            <label className="field">
              Турнір
              <select
                {...register("tournamentId")}
                className="input"
              >
                <option value="">Оберіть турнір</option>
                {tournaments.map((tournament) => (
                  <option key={tournament.id} value={tournament.id}>
                    {tournament.name} ({tournament.game})
                  </option>
                ))}
              </select>
              {errors.tournamentId && (
                <p className="field-error">{errors.tournamentId.message}</p>
              )}
            </label>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="field">
                Домашня команда
                <select
                  {...register("homeTeamId")}
                  className="input"
                >
                  <option value="">Оберіть команду</option>
                  {teams.map((team) => (
                    <option key={team.id} value={team.id}>
                      {team.name} ({team.tag})
                    </option>
                  ))}
                </select>
              </label>
              <label className="field">
                Гостьова команда
                <select
                  {...register("awayTeamId")}
                  className="input"
                >
                  <option value="">Оберіть команду</option>
                  {teams.map((team) => (
                    <option key={team.id} value={team.id}>
                      {team.name} ({team.tag})
                    </option>
                  ))}
                </select>
              </label>
            </div>
          </>
        )}
        <label className="field">
          Час початку
          <input
            type="datetime-local"
            {...register("scheduledAt")}
            className="input"
          />
          {errors.scheduledAt && <p className="field-error">{errors.scheduledAt.message}</p>}
        </label>
        {!id && (
          <>
            <label className="field">
              Тип матчу
              <input
                type="text"
                {...register("matchType")}
                className="input"
              />
            </label>
            <label className="field">
              Формат
              <select
                {...register("format")}
                className="input"
              >
                <option value="BO1">BO1</option>
                <option value="BO3">BO3</option>
                <option value="BO5">BO5</option>
              </select>
            </label>
          </>
        )}
        <label className="field">
          Примітки
          <textarea
            rows={3}
            {...register("notes")}
            className="input"
          />
        </label>
        {id && (
          <>
            <label className="field">
              Статус
              <select
                {...register("status")}
                className="input"
              >
                <option value="Scheduled">Заплановано</option>
                <option value="InProgress">У процесі</option>
                <option value="Completed">Завершено</option>
                <option value="Cancelled">Скасовано</option>
              </select>
            </label>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="field">
                Рахунок (домашні)
                <input
                  type="number"
                  {...register("homeTeamScore")}
                  className="input"
                />
              </label>
              <label className="field">
                Рахунок (гості)
                <input
                  type="number"
                  {...register("awayTeamScore")}
                  className="input"
                />
              </label>
            </div>
            <label className="field">
              Переможець
              <select
                {...register("winnerTeamId")}
                className="input"
              >
                <option value="">Не визначено</option>
                {match?.homeTeam && <option value={match.homeTeam.id}>{match.homeTeam.name}</option>}
                {match?.awayTeam && <option value={match.awayTeam.id}>{match.awayTeam.name}</option>}
              </select>
            </label>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="field">
                Старт матчу
                <input
                  type="datetime-local"
                  {...register("startedAt")}
                  className="input"
                />
              </label>
              <label className="field">
                Завершення матчу
                <input
                  type="datetime-local"
                  {...register("endedAt")}
                  className="input"
                />
              </label>
            </div>
          </>
        )}
        {loading && <div className="text-micro text-text-faint">Завантаження даних...</div>}
        <div className="flex items-center gap-3 border-t border-line-soft pt-5">
          <button
            type="submit"
            disabled={isSubmitting}
            className="btn btn-primary"
          >
            {isSubmitting ? "Збереження..." : "Зберегти"}
          </button>
          <button
            type="button"
            onClick={() => navigate("/matches")}
            className="btn btn-secondary"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default MatchForm;
