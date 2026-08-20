import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { matchesApi } from "../../api/matchesApi";
import { teamsApi } from "../../api/teamsApi";
import { tournamentsApi } from "../../api/tournamentsApi";
import { GAMES, gameLabel } from "../../constants/games";
import { MATCH_TYPES, matchTypeLabel } from "../../constants/matchTypes";
import { useIsRole } from "../../hooks/useEffectiveRole";
import { DateTimePicker } from "../../components/ui/DateTimePicker";
import { useSubmitError } from "../../hooks/useSubmitError";
import type { CreateMatchDto, MatchDto, TeamRowDto, TournamentDto, UpdateMatchDto } from "../../types";

const schema = z.object({
  tournamentId: z.coerce.number().optional(),
  /** Лише для практичного матчу — у турнірному дисципліну диктує турнір. */
  game: z.string().optional(),
  name: z.string().max(100, "Максимум 100 символів").optional(),
  homeTeamId: z.coerce.number().optional(),
  awayTeamId: z.coerce.number().optional(),
  scheduledAt: z.string().min(1, "Вкажіть час"),
  matchType: z.string().optional(),
  format: z.string().optional(),
  notes: z.string().optional(),
  streamUrl: z.string().optional(),
  trackerUrl: z.string().optional(),
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
  const [teams, setTeams] = useState<TeamRowDto[]>([]);
  const [match, setMatch] = useState<MatchDto | null>(null);
  const {
    register,
    control,
    handleSubmit,
    setValue,
    setError,
    watch,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  // Дисципліну визначає турнір — показуємо її лише для читання, щоб було видно,
  // під яку гру створюється матч.
  // Турнір веде організатор, тож і матчі в нього додає він. Капітан ставить
  // тільки практичний матч — вибору турніру він просто не бачить, а сервер
  // однаково перевіряє те саме через Common/MatchCreationPolicy.cs.
  const canUseTournament = useIsRole("Admin", "Organizer");

  const selectedTournamentId = watch("tournamentId");
  const selectedTournament = tournaments.find(
    (tournament) => tournament.id === Number(selectedTournamentId)
  );

  // Турніри й команди підвантажуються списками, щоб не вводити ID вручну
  useEffect(() => {
    let isActive = true;

    Promise.all([
      tournamentsApi.getAllActive().catch(() => [] as TournamentDto[]),
      teamsApi.getPaged({ page: 1, pageSize: 100 }).then((r) => r.data).catch(() => [] as TeamRowDto[])
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
        setValue("streamUrl", data.streamUrl ?? "");
        setValue("trackerUrl", data.trackerUrl ?? "");
        setValue("startedAt", data.startedAt?.slice(0, 16));
        setValue("endedAt", data.endedAt?.slice(0, 16));
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [id, setValue]);

  const submitError = useSubmitError<FormValues>(setError);

  const onSubmit = async (values: FormValues) => {
    submitError.clear();
    try {
    if (!id) {
      // Практичний матч — це матч без турніру. Дисципліну тоді треба назвати:
      // успадкувати її нема від чого, а матч без гри не видно у фільтрі.
      const isFriendly = !canUseTournament || !values.tournamentId;

      if (isFriendly && !values.game) {
        setError("game", { message: "Оберіть дисципліну" });
        return;
      }

      // Турнірний матч організатор і далі складає з двох названих команд:
      // порожнього місця в сітці не буває.
      if (!isFriendly && (!values.homeTeamId || !values.awayTeamId)) {
        setError("homeTeamId", { message: "Оберіть обидві команди" });
        return;
      }

      if (values.homeTeamId && values.homeTeamId === values.awayTeamId) {
        setError("awayTeamId", { message: "Команда не може грати сама із собою" });
        return;
      }

      const payload: CreateMatchDto = {
        tournamentId: isFriendly ? null : (values.tournamentId ?? null),
        game: isFriendly ? values.game : null,
        name: values.name?.trim() || null,
        // Практичний матч капітан ставить від імені своєї команди — її
        // підставляє сервер — і лишає відкритим: гостя назве той, хто
        // приєднається.
        homeTeamId: isFriendly ? null : values.homeTeamId,
        awayTeamId: isFriendly ? null : values.awayTeamId,
        scheduledAt: values.scheduledAt,
        matchType: values.matchType ?? "GroupStage",
        format: values.format ?? "BO1",
        notes: values.notes ?? "",
        streamUrl: values.streamUrl?.trim() || null,
        trackerUrl: values.trackerUrl?.trim() || null
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
        streamUrl: values.streamUrl?.trim() || null,
        trackerUrl: values.trackerUrl?.trim() || null,
        startedAt: values.startedAt ?? null,
        endedAt: values.endedAt ?? null
      };
      await matchesApi.update(Number(id), payload);
    }

      navigate("/matches");
    } catch (caught) {
      submitError.capture(caught);
    }
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
            {/* Практичний матч ніщо не називає, крім самої назви: турніру
                в нього немає, а команду-гостя ще не обрано. */}
            <label className="field">
              Назва матчу
              <input
                type="text"
                {...register("name")}
                placeholder="Вечірній скрим"
                className="input"
              />
              {errors.name && <p className="field-error">{errors.name.message}</p>}
            </label>

            {canUseTournament && (
              <label className="field">
                Турнір
                <select
                  {...register("tournamentId")}
                  className="input"
                >
                  <option value="">Без турніру — практичний матч</option>
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
            )}
            {canUseTournament && selectedTournament ? (
              <div className="field">
                Дисципліна
                <div className="input flex items-center text-text-muted">
                  {gameLabel(selectedTournament.game)}
                </div>
                <p className="field-hint">Дисципліну визначає турнір — окремо її не обирають.</p>
              </div>
            ) : (
              <label className="field">
                Дисципліна
                <select {...register("game")} className="input" defaultValue="">
                  <option value="">Оберіть дисципліну</option>
                  {GAMES.map((game) => (
                    <option key={game} value={game}>
                      {gameLabel(game)}
                    </option>
                  ))}
                </select>
                {errors.game && <p className="field-error">{errors.game.message}</p>}
                <p className="field-hint">
                  Практичний матч не дає титулів і не змінює рейтинг.
                </p>
              </label>
            )}
            {!canUseTournament || !selectedTournament ? (
              <p className="rounded-lg border border-line bg-ink-800/60 px-3 py-2.5 text-micro text-text-muted">
                Матч буде відкритим: за вашу команду його поставить сервер, а
                суперника назве капітан, який приєднається.
              </p>
            ) : (
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
                {errors.homeTeamId && <p className="field-error">{errors.homeTeamId.message}</p>}
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
                {errors.awayTeamId && <p className="field-error">{errors.awayTeamId.message}</p>}
              </label>
            </div>
            )}
          </>
        )}
        <div className="field">
          Час початку
          <div className="mt-1.5">
            <Controller
              control={control}
              name="scheduledAt"
              render={({ field }) => (
                <DateTimePicker
                  ariaLabel="Час початку матчу"
                  value={field.value ?? ""}
                  onChange={field.onChange}
                />
              )}
            />
          </div>
          {errors.scheduledAt && <p className="field-error">{errors.scheduledAt.message}</p>}
        </div>
        {!id && (
          <>
            {/* Стадія — це місце в турнірній сітці, тож поза турніром її
                немає: практичному матчу сервер ставить GroupStage сам. */}
            {canUseTournament && selectedTournament && (
              <label className="field">
                Стадія
                <select {...register("matchType")} className="input">
                  {MATCH_TYPES.map((value) => (
                    <option key={value} value={value}>
                      {matchTypeLabel(value)}
                    </option>
                  ))}
                </select>
                <p className="field-hint">
                  Місце в турнірній сітці. Фінал і матч за третє місце важать у рейтингу більше.
                </p>
              </label>
            )}
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
          Посилання на трансляцію
          <input type="url" placeholder="https://twitch.tv/..." {...register("streamUrl")} className="input" />
          {errors.streamUrl ? (
            <p className="field-error">{errors.streamUrl.message}</p>
          ) : (
            <p className="field-hint">Twitch або YouTube, обовʼязково через https://</p>
          )}
        </label>
        <label className="field">
          Матч у трекері статистики
          <input
            type="url"
            placeholder="https://tracker.gg/valorant/match/..."
            {...register("trackerUrl")}
            className="input"
          />
          {errors.trackerUrl ? (
            <p className="field-error">{errors.trackerUrl.message}</p>
          ) : (
            <p className="field-hint">Необовʼязково. Будь-яке https-посилання на сторінку матчу.</p>
          )}
        </label>
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
            <div className="field">
              Дисципліна
              <div className="input flex items-center text-text-muted">
                {match ? gameLabel(match.game) : "—"}
              </div>
            </div>
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
              <div className="field">
                Старт матчу
                <div className="mt-1.5">
                  <Controller
                    control={control}
                    name="startedAt"
                    render={({ field }) => (
                      <DateTimePicker
                        ariaLabel="Фактичний старт матчу"
                        value={field.value ?? ""}
                        onChange={field.onChange}
                      />
                    )}
                  />
                </div>
              </div>
              <div className="field">
                Завершення матчу
                <div className="mt-1.5">
                  <Controller
                    control={control}
                    name="endedAt"
                    render={({ field }) => (
                      <DateTimePicker
                        ariaLabel="Фактичне завершення матчу"
                        value={field.value ?? ""}
                        onChange={field.onChange}
                      />
                    )}
                  />
                </div>
              </div>
            </div>
          </>
        )}
        {loading && <div className="text-micro text-text-faint">Завантаження даних...</div>}
        {submitError.error && <div className="notice notice-error">{submitError.error}</div>}
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
