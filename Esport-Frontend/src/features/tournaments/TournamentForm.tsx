import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { tournamentsApi } from "../../api/tournamentsApi";
import type { CreateTournamentDto, UpdateTournamentDto } from "../../types";

const schema = z.object({
  name: z.string().min(3, "Вкажіть назву турніру"),
  description: z.string().min(10, "Додайте короткий опис"),
  game: z.string().min(2, "Вкажіть дисципліну"),
  startDate: z.string().min(1, "Оберіть дату старту"),
  endDate: z.string().min(1, "Оберіть дату завершення"),
  maxTeams: z.coerce.number().min(2, "Мінімум 2 команди"),
  prizePool: z.coerce.number().min(0, "Призовий фонд має бути додатним"),
  organizerId: z.coerce.number().optional(),
  status: z.string().min(1, "Оберіть статус")
});

type FormValues = z.infer<typeof schema>;

const TournamentForm = () => {
  const navigate = useNavigate();
  const { id } = useParams();
  const [isLoading, setIsLoading] = useState(false);
  const {
    register,
    handleSubmit,
    setValue,
    setError,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      status: "Planned"
    }
  });

  useEffect(() => {
    if (!id) {
      return;
    }

    const loadTournament = async () => {
      setIsLoading(true);
      try {
        const data = await tournamentsApi.getById(Number(id));
        setValue("name", data.name);
        setValue("description", data.description);
        setValue("game", data.game);
        setValue("startDate", data.startDate.slice(0, 10));
        setValue("endDate", data.endDate.slice(0, 10));
        setValue("maxTeams", data.maxTeams);
        setValue("prizePool", Number(data.prizePool));
        setValue("status", data.status);
      } finally {
        setIsLoading(false);
      }
    };

    loadTournament();
  }, [id, setValue]);

  const onSubmit = async (values: FormValues) => {
    if (!id && !values.organizerId) {
      setError("organizerId", { message: "Вкажіть організатора" });
      return;
    }
    if (id) {
      const payload: UpdateTournamentDto = {
        name: values.name,
        description: values.description,
        startDate: values.startDate,
        endDate: values.endDate,
        maxTeams: values.maxTeams,
        prizePool: values.prizePool,
        status: values.status
      };
      await tournamentsApi.update(Number(id), payload);
    } else {
      const payload: CreateTournamentDto = {
        name: values.name,
        description: values.description,
        game: values.game,
        startDate: values.startDate,
        endDate: values.endDate,
        maxTeams: values.maxTeams,
        prizePool: values.prizePool,
        organizerId: values.organizerId ?? 0
      };
      await tournamentsApi.create(payload);
    }
    navigate("/tournaments");
  };

  return (
    <div className="max-w-2xl space-y-6">
      <header>
        <h1 className="text-3xl font-semibold">{id ? "Редагування турніру" : "Новий турнір"}</h1>
        <p className="mt-2 text-sm text-slate-400">Заповніть ключові параметри змагання.</p>
      </header>
      <form onSubmit={handleSubmit(onSubmit)} className="glass-panel rounded-2xl p-6 space-y-4">
        <label className="block text-sm">
          Назва
          <input
            type="text"
            {...register("name")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.name && <p className="mt-1 text-xs text-neon-magenta">{errors.name.message}</p>}
        </label>
        <label className="block text-sm">
          Опис
          <textarea
            rows={4}
            {...register("description")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.description && <p className="mt-1 text-xs text-neon-magenta">{errors.description.message}</p>}
        </label>
        {!id && (
          <label className="block text-sm">
            Дисципліна
            <input
              type="text"
              {...register("game")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.game && <p className="mt-1 text-xs text-neon-magenta">{errors.game.message}</p>}
          </label>
        )}
        <div className="grid gap-4 md:grid-cols-2">
          <label className="block text-sm">
            Дата старту
            <input
              type="date"
              {...register("startDate")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.startDate && <p className="mt-1 text-xs text-neon-magenta">{errors.startDate.message}</p>}
          </label>
          <label className="block text-sm">
            Дата завершення
            <input
              type="date"
              {...register("endDate")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.endDate && <p className="mt-1 text-xs text-neon-magenta">{errors.endDate.message}</p>}
          </label>
        </div>
        <label className="block text-sm">
          Максимум команд
          <input
            type="number"
            {...register("maxTeams")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.maxTeams && <p className="mt-1 text-xs text-neon-magenta">{errors.maxTeams.message}</p>}
        </label>
        <label className="block text-sm">
          Призовий фонд (USD)
          <input
            type="number"
            {...register("prizePool")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.prizePool && <p className="mt-1 text-xs text-neon-magenta">{errors.prizePool.message}</p>}
        </label>
        {!id && (
          <label className="block text-sm">
            ID організатора
            <input
              type="number"
              {...register("organizerId")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.organizerId && <p className="mt-1 text-xs text-neon-magenta">{errors.organizerId.message}</p>}
          </label>
        )}
        <label className="block text-sm">
          Статус
          <select
            {...register("status")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          >
            <option value="Planned">Заплановано</option>
            <option value="InProgress">У процесі</option>
            <option value="Completed">Завершено</option>
          </select>
        </label>
        {isLoading && <div className="text-xs text-slate-400">Завантаження даних...</div>}
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
            onClick={() => navigate("/tournaments")}
            className="rounded-xl border border-white/10 px-4 py-2 text-sm text-slate-300"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default TournamentForm;
