import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { playersApi } from "../../api/playersApi";
import type { CreatePlayerDto, UpdatePlayerDto } from "../../types";

const schema = z.object({
  nickname: z.string().min(2, "Вкажіть нікнейм"),
  position: z.string().min(2, "Вкажіть позицію"),
  country: z.string().min(2, "Вкажіть країну"),
  age: z.coerce.number().min(12, "Мінімум 12 років"),
  userId: z.coerce.number().optional(),
  teamId: z.coerce.number().optional()
});

type FormValues = z.infer<typeof schema>;

const PlayerForm = () => {
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
        const data = await playersApi.getById(Number(id));
        setValue("nickname", data.nickname);
        setValue("position", data.position);
        setValue("country", data.country);
        setValue("age", data.age);
        setValue("teamId", data.team?.id ?? undefined);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [id, setValue]);

  const onSubmit = async (values: FormValues) => {
    if (!id && !values.userId) {
      setError("userId", { message: "Вкажіть користувача" });
      return;
    }

    if (id) {
      const payload: UpdatePlayerDto = {
        position: values.position,
        country: values.country,
        age: values.age,
        teamId: values.teamId ?? null
      };
      await playersApi.update(Number(id), payload);
    } else {
      const payload: CreatePlayerDto = {
        nickname: values.nickname,
        position: values.position,
        country: values.country,
        age: values.age,
        userId: values.userId ?? 0,
        teamId: values.teamId ?? null
      };
      await playersApi.create(payload);
    }

    navigate("/players");
  };

  return (
    <div className="max-w-2xl space-y-6">
      <header>
        <h1 className="text-3xl font-semibold">{id ? "Редагування гравця" : "Новий гравець"}</h1>
        <p className="mt-2 text-sm text-slate-400">Заповніть персональні дані та склад.</p>
      </header>
      <form onSubmit={handleSubmit(onSubmit)} className="glass-panel rounded-2xl p-6 space-y-4">
        {!id && (
          <label className="block text-sm">
            Нікнейм
            <input
              type="text"
              {...register("nickname")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.nickname && <p className="mt-1 text-xs text-neon-magenta">{errors.nickname.message}</p>}
          </label>
        )}
        <label className="block text-sm">
          Позиція
          <input
            type="text"
            {...register("position")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.position && <p className="mt-1 text-xs text-neon-magenta">{errors.position.message}</p>}
        </label>
        <label className="block text-sm">
          Країна
          <input
            type="text"
            {...register("country")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.country && <p className="mt-1 text-xs text-neon-magenta">{errors.country.message}</p>}
        </label>
        <label className="block text-sm">
          Вік
          <input
            type="number"
            {...register("age")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.age && <p className="mt-1 text-xs text-neon-magenta">{errors.age.message}</p>}
        </label>
        {!id && (
          <label className="block text-sm">
            ID користувача
            <input
              type="number"
              {...register("userId")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.userId && <p className="mt-1 text-xs text-neon-magenta">{errors.userId.message}</p>}
          </label>
        )}
        <label className="block text-sm">
          ID команди (необов'язково)
          <input
            type="number"
            {...register("teamId")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
        </label>
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
            onClick={() => navigate("/players")}
            className="rounded-xl border border-white/10 px-4 py-2 text-sm text-slate-300"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default PlayerForm;
