import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { matchesApi } from "../../api/matchesApi";
import type { CreateMatchDto, UpdateMatchDto } from "../../types";

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
  const {
    register,
    handleSubmit,
    setValue,
    setError,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  useEffect(() => {
    if (!id) {
      return;
    }

    const load = async () => {
      setLoading(true);
      try {
        const data = await matchesApi.getById(Number(id));
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
        winnerTeamId: values.winnerTeamId ?? null,
        notes: values.notes ?? "",
        startedAt: values.startedAt ?? null,
        endedAt: values.endedAt ?? null
      };
      await matchesApi.update(Number(id), payload);
    }

    navigate("/matches");
  };

  return (
    <div className="max-w-2xl space-y-6">
      <header>
        <h1 className="text-3xl font-semibold">{id ? "Редагування матчу" : "Новий матч"}</h1>
        <p className="mt-2 text-sm text-slate-400">Контролюйте розклад та результати матчів.</p>
      </header>
      <form onSubmit={handleSubmit(onSubmit)} className="glass-panel rounded-2xl p-6 space-y-4">
        {!id && (
          <>
            <label className="block text-sm">
              ID турніру
              <input
                type="number"
                {...register("tournamentId")}
                className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
              />
            </label>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="block text-sm">
                ID домашньої команди
                <input
                  type="number"
                  {...register("homeTeamId")}
                  className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
                />
              </label>
              <label className="block text-sm">
                ID гостьової команди
                <input
                  type="number"
                  {...register("awayTeamId")}
                  className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
                />
              </label>
            </div>
          </>
        )}
        <label className="block text-sm">
          Час початку
          <input
            type="datetime-local"
            {...register("scheduledAt")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.scheduledAt && <p className="mt-1 text-xs text-neon-magenta">{errors.scheduledAt.message}</p>}
        </label>
        {!id && (
          <>
            <label className="block text-sm">
              Тип матчу
              <input
                type="text"
                {...register("matchType")}
                className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
              />
            </label>
            <label className="block text-sm">
              Формат
              <select
                {...register("format")}
                className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
              >
                <option value="BO1">BO1</option>
                <option value="BO3">BO3</option>
                <option value="BO5">BO5</option>
              </select>
            </label>
          </>
        )}
        <label className="block text-sm">
          Примітки
          <textarea
            rows={3}
            {...register("notes")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
        </label>
        {id && (
          <>
            <label className="block text-sm">
              Статус
              <select
                {...register("status")}
                className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
              >
                <option value="Scheduled">Заплановано</option>
                <option value="InProgress">У процесі</option>
                <option value="Completed">Завершено</option>
                <option value="Cancelled">Скасовано</option>
              </select>
            </label>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="block text-sm">
                Рахунок (домашні)
                <input
                  type="number"
                  {...register("homeTeamScore")}
                  className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
                />
              </label>
              <label className="block text-sm">
                Рахунок (гості)
                <input
                  type="number"
                  {...register("awayTeamScore")}
                  className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
                />
              </label>
            </div>
            <label className="block text-sm">
              ID переможця (необов'язково)
              <input
                type="number"
                {...register("winnerTeamId")}
                className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
              />
            </label>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="block text-sm">
                Старт матчу
                <input
                  type="datetime-local"
                  {...register("startedAt")}
                  className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
                />
              </label>
              <label className="block text-sm">
                Завершення матчу
                <input
                  type="datetime-local"
                  {...register("endedAt")}
                  className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
                />
              </label>
            </div>
          </>
        )}
        {loading && <div className="text-xs text-slate-400">Завантаження даних...</div>}
        <div className="flex items-center gap-3">
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-xl bg-neon-cyan/90 px-4 py-2 text-sm font-semibold text-night-900 hover:bg-neon-cyan"
          >
            {isSubmitting ? "Збереження..." : "Зберегти"}
          </button>
          <button
            type="button"
            onClick={() => navigate("/matches")}
            className="rounded-xl border border-white/10 px-4 py-2 text-sm text-slate-300"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default MatchForm;
