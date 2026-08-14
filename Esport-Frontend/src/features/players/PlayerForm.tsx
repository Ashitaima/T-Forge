import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { playersApi } from "../../api/playersApi";
import { teamsApi } from "../../api/teamsApi";
import type { CreatePlayerDto, TeamDto, UpdatePlayerDto } from "../../types";

const schema = z.object({
  nickname: z.string().min(2, "Вкажіть нікнейм"),
  position: z.string().min(2, "Вкажіть позицію"),
  country: z.string().min(2, "Вкажіть країну"),
  age: z.coerce.number().min(12, "Мінімум 12 років"),
  teamId: z.coerce.number().optional()
});

type FormValues = z.infer<typeof schema>;

const PlayerForm = () => {
  const navigate = useNavigate();
  const { id } = useParams();
  const [loading, setLoading] = useState(false);
  const [teams, setTeams] = useState<TeamDto[]>([]);
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  // Команди підвантажуються списком, щоб не вводити ID вручну
  useEffect(() => {
    let isActive = true;
    teamsApi
      .getPaged({ page: 1, pageSize: 100 })
      .then((response) => {
        if (isActive) {
          setTeams(response.data);
        }
      })
      .catch(() => undefined);

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
        teamId: values.teamId ? Number(values.teamId) : null
      };
      await playersApi.create(payload);
    }

    navigate("/players");
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <header className="border-b border-line-soft pb-5">
        <h1 className="page-title">{id ? "Редагування гравця" : "Новий гравець"}</h1>
        <p className="muted mt-2 text-body">Заповніть персональні дані та склад.</p>
      </header>
      <form onSubmit={handleSubmit(onSubmit)} className="panel panel-body space-y-5">
        {!id && (
          <label className="field">
            Нікнейм
            <input
              type="text"
              {...register("nickname")}
              className="input"
            />
            {errors.nickname && <p className="field-error">{errors.nickname.message}</p>}
          </label>
        )}
        <label className="field">
          Позиція
          <input
            type="text"
            {...register("position")}
            className="input"
          />
          {errors.position && <p className="field-error">{errors.position.message}</p>}
        </label>
        <label className="field">
          Країна
          <input
            type="text"
            {...register("country")}
            className="input"
          />
          {errors.country && <p className="field-error">{errors.country.message}</p>}
        </label>
        <label className="field">
          Вік
          <input
            type="number"
            {...register("age")}
            className="input"
          />
          {errors.age && <p className="field-error">{errors.age.message}</p>}
        </label>
        {!id && (
          <p className="rounded-lg border border-line bg-ink-800/60 px-3 py-2.5 text-micro text-text-muted">
            Профіль буде прив'язано до поточного користувача — вказувати ID вручну не потрібно.
          </p>
        )}
        <label className="field">
          Команда (необов'язково)
          <select
            {...register("teamId")}
            className="input"
          >
            <option value="">Без команди</option>
            {teams.map((team) => (
              <option key={team.id} value={team.id}>
                {team.name} ({team.tag})
              </option>
            ))}
          </select>
        </label>
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
            onClick={() => navigate("/players")}
            className="btn btn-secondary"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default PlayerForm;
