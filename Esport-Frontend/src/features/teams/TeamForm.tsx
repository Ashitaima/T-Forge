import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { teamsApi } from "../../api/teamsApi";
import type { CreateTeamDto, UpdateTeamDto } from "../../types";

const schema = z.object({
  name: z.string().min(2, "Вкажіть назву команди"),
  tag: z.string().min(2, "Вкажіть тег"),
  description: z.string().min(5, "Додайте короткий опис"),
  region: z.string().min(2, "Вкажіть регіон"),
  captainId: z.coerce.number().optional()
});

type FormValues = z.infer<typeof schema>;

const TeamForm = () => {
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
        const data = await teamsApi.getById(Number(id));
        setValue("name", data.name);
        setValue("tag", data.tag);
        setValue("description", data.description);
        setValue("region", data.region);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [id, setValue]);

  const onSubmit = async (values: FormValues) => {
    if (!id && !values.captainId) {
      setError("captainId", { message: "Вкажіть капітана" });
      return;
    }

    if (id) {
      const payload: UpdateTeamDto = {
        name: values.name,
        tag: values.tag,
        description: values.description,
        region: values.region
      };
      await teamsApi.update(Number(id), payload);
    } else {
      const payload: CreateTeamDto = {
        name: values.name,
        tag: values.tag,
        description: values.description,
        region: values.region,
        captainId: values.captainId ?? 0
      };
      await teamsApi.create(payload);
    }

    navigate("/teams");
  };

  return (
    <div className="max-w-2xl space-y-6">
      <header>
        <h1 className="text-3xl font-semibold">{id ? "Редагування команди" : "Нова команда"}</h1>
        <p className="mt-2 text-sm text-slate-400">Заповніть профіль команди та дані капітана.</p>
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
          Тег
          <input
            type="text"
            {...register("tag")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.tag && <p className="mt-1 text-xs text-neon-magenta">{errors.tag.message}</p>}
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
        <label className="block text-sm">
          Регіон
          <input
            type="text"
            {...register("region")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.region && <p className="mt-1 text-xs text-neon-magenta">{errors.region.message}</p>}
        </label>
        {!id && (
          <label className="block text-sm">
            ID капітана
            <input
              type="number"
              {...register("captainId")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.captainId && <p className="mt-1 text-xs text-neon-magenta">{errors.captainId.message}</p>}
          </label>
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
            onClick={() => navigate("/teams")}
            className="rounded-xl border border-white/10 px-4 py-2 text-sm text-slate-300"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default TeamForm;
