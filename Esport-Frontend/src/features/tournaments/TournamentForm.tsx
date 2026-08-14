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
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      status: "Registration"
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
        prizePool: values.prizePool
      };
      await tournamentsApi.create(payload);
    }
    navigate("/tournaments");
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <header className="border-b border-line-soft pb-5">
        <h1 className="page-title">{id ? "Редагування турніру" : "Новий турнір"}</h1>
        <p className="muted mt-2 text-body">Заповніть ключові параметри змагання.</p>
      </header>
      <form onSubmit={handleSubmit(onSubmit)} className="panel panel-body space-y-5">
        <label className="field">
          Назва
          <input
            type="text"
            {...register("name")}
            className="input"
          />
          {errors.name && <p className="field-error">{errors.name.message}</p>}
        </label>
        <label className="field">
          Опис
          <textarea
            rows={4}
            {...register("description")}
            className="input"
          />
          {errors.description && <p className="field-error">{errors.description.message}</p>}
        </label>
        {!id && (
          <label className="field">
            Дисципліна
            <input
              type="text"
              {...register("game")}
              className="input"
            />
            {errors.game && <p className="field-error">{errors.game.message}</p>}
          </label>
        )}
        <div className="grid gap-4 md:grid-cols-2">
          <label className="field">
            Дата старту
            <input
              type="date"
              {...register("startDate")}
              className="input"
            />
            {errors.startDate && <p className="field-error">{errors.startDate.message}</p>}
          </label>
          <label className="field">
            Дата завершення
            <input
              type="date"
              {...register("endDate")}
              className="input"
            />
            {errors.endDate && <p className="field-error">{errors.endDate.message}</p>}
          </label>
        </div>
        <label className="field">
          Максимум команд
          <input
            type="number"
            {...register("maxTeams")}
            className="input"
          />
          {errors.maxTeams && <p className="field-error">{errors.maxTeams.message}</p>}
        </label>
        <label className="field">
          Призовий фонд (USD)
          <input
            type="number"
            {...register("prizePool")}
            className="input"
          />
          {errors.prizePool && <p className="field-error">{errors.prizePool.message}</p>}
        </label>
        {!id && (
          <p className="rounded-lg border border-line bg-ink-800/60 px-3 py-2.5 text-micro text-text-muted">
            Організатором стане поточний користувач — вказувати ID вручну не потрібно.
          </p>
        )}
        <label className="field">
          Статус
          <select
            {...register("status")}
            className="input"
          >
            <option value="Registration">Реєстрація</option>
            <option value="InProgress">У процесі</option>
            <option value="Completed">Завершено</option>
            <option value="Cancelled">Скасовано</option>
          </select>
        </label>
        {isLoading && <div className="text-micro text-text-faint">Завантаження даних...</div>}
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
            onClick={() => navigate("/tournaments")}
            className="btn btn-secondary"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default TournamentForm;
