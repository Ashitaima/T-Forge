import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { useSubmitError } from "../../hooks/useSubmitError";
import { tournamentsApi } from "../../api/tournamentsApi";
import { GAMES, GAME_LABELS } from "../../constants/games";
import { DateTimePicker } from "../../components/ui/DateTimePicker";
import type { CreateTournamentDto, UpdateTournamentDto } from "../../types";

const schema = z.object({
  name: z.string().min(3, "Вкажіть назву турніру"),
  description: z.string().min(10, "Додайте короткий опис"),
  // errorMap, а не required_error: порожній рядок із <select> — це не
  // «поле відсутнє», а «значення поза переліком», і без цього користувач
  // бачив би службове «Invalid enum value ... received ''».
  game: z.enum(GAMES, { errorMap: () => ({ message: "Оберіть дисципліну" }) }),
  startDate: z.string().min(1, "Оберіть дату старту"),
  endDate: z.string().min(1, "Оберіть дату завершення"),
  maxTeams: z.coerce.number().min(2, "Мінімум 2 команди"),
  prizePool: z.coerce.number().min(0, "Призовий фонд має бути додатним"),
  isInviteOnly: z.boolean(),
  // Статус є лише в редагуванні: новий турнір завжди починається з реєстрації,
  // і цей статус проставляє сервер.
  status: z.string().optional()
});

type FormValues = z.infer<typeof schema>;

const TournamentForm = () => {
  const navigate = useNavigate();
  const { id } = useParams();
  const [isLoading, setIsLoading] = useState(false);
  const {
    register,
    control,
    handleSubmit,
    setValue,
    setError,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      isInviteOnly: false,
      status: "Registration"
    }
  });

  const submitError = useSubmitError<FormValues>(setError);

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
        // Бекенд може повернути й невідоме значення (старі дані) — форма
        // редагування дисципліну не надсилає, тож це лише для показу.
        setValue("game", data.game as FormValues["game"]);
        setValue("startDate", data.startDate.slice(0, 10));
        setValue("endDate", data.endDate.slice(0, 10));
        setValue("maxTeams", data.maxTeams);
        setValue("prizePool", Number(data.prizePool));
        setValue("isInviteOnly", data.isInviteOnly);
        setValue("status", data.status);
      } finally {
        setIsLoading(false);
      }
    };

    loadTournament();
  }, [id, setValue]);

  /** Сам запит. Помилку показує onSubmit нижче. */
  const save = async (values: FormValues) => {
    if (id) {
      const payload: UpdateTournamentDto = {
        name: values.name,
        description: values.description,
        startDate: values.startDate,
        endDate: values.endDate,
        maxTeams: values.maxTeams,
        prizePool: values.prizePool,
        isInviteOnly: values.isInviteOnly,
        status: values.status ?? "Registration"
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
        isInviteOnly: values.isInviteOnly
      };
      await tournamentsApi.create(payload);
    }
  };

  const onSubmit = async (values: FormValues) => {
    submitError.clear();
    try {
      await save(values);
      navigate("/tournaments");
    } catch (caught) {
      submitError.capture(caught);
    }
  };

  const today = new Date().toISOString().slice(0, 10);

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
            <select {...register("game")} className="input" defaultValue="">
              <option value="">Оберіть дисципліну</option>
              {GAMES.map((game) => (
                <option key={game} value={game}>
                  {GAME_LABELS[game]}
                </option>
              ))}
            </select>
            {errors.game ? (
              <p className="field-error">{errors.game.message}</p>
            ) : (
              <p className="field-hint">Після створення турніру дисципліну змінити не можна.</p>
            )}
          </label>
        )}
        <div className="grid gap-4 md:grid-cols-2">
          <div className="field">
            Дата старту
            <div className="mt-1.5">
              <Controller
                control={control}
                name="startDate"
                render={({ field }) => (
                  <DateTimePicker
                    mode="date"
                    ariaLabel="Дата старту"
                    minDate={id ? undefined : today}
                    value={field.value ?? ""}
                    onChange={field.onChange}
                  />
                )}
              />
            </div>
            {errors.startDate && <p className="field-error">{errors.startDate.message}</p>}
          </div>
          <div className="field">
            Дата завершення
            <div className="mt-1.5">
              <Controller
                control={control}
                name="endDate"
                render={({ field }) => (
                  <DateTimePicker
                    mode="date"
                    ariaLabel="Дата завершення"
                    minDate={id ? undefined : today}
                    value={field.value ?? ""}
                    onChange={field.onChange}
                  />
                )}
              />
            </div>
            {errors.endDate && <p className="field-error">{errors.endDate.message}</p>}
          </div>
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

        {/* Закритий турнір: реєстрація перестає бути самообслуговуванням */}
        <div className="surface-raised flex items-start gap-3 px-4 py-3.5">
          <input
            id="invite-only"
            type="checkbox"
            {...register("isInviteOnly")}
            className="mt-0.5 h-4 w-4 shrink-0 accent-ember"
          />
          <label htmlFor="invite-only" className="min-w-0">
            <span className="text-body font-medium text-text">Тільки за запрошеннями</span>
            <span className="muted mt-1 block text-micro">
              Капітани не реєструються самі. Ви запрошуєте команди, або вони подають заявку,
              яку ви схвалюєте.
            </span>
          </label>
        </div>

        {!id && (
          <p className="rounded-lg border border-line bg-ink-800/60 px-3 py-2.5 text-micro text-text-muted">
            Організатором стане поточний користувач — вказувати ID вручну не потрібно.
            Турнір починається зі статусу «Реєстрація».
          </p>
        )}

        {/* Статус — властивість турніру, що вже існує: новий завжди
            починається з реєстрації, і обирати тут нема чого. */}
        {id && (
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
        )}
        {isLoading && <div className="text-micro text-text-faint">Завантаження даних...</div>}
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
