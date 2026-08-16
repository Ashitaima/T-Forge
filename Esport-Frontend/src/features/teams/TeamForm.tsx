import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { useSubmitError } from "../../hooks/useSubmitError";
import { teamsApi } from "../../api/teamsApi";
import type { CreateTeamDto, UpdateTeamDto } from "../../types";

const schema = z.object({
  name: z.string().min(2, "Вкажіть назву команди"),
  tag: z.string().min(2, "Вкажіть тег"),
  description: z.string().min(5, "Додайте короткий опис"),
  region: z.string().min(2, "Вкажіть регіон")
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

  const submitError = useSubmitError<FormValues>(setError);

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

  /** Сам запит. Помилку показує onSubmit нижче. */
  const save = async (values: FormValues) => {
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
        region: values.region
      };
      await teamsApi.create(payload);
    }
  };

  const onSubmit = async (values: FormValues) => {
    submitError.clear();
    try {
      await save(values);
      navigate("/teams");
    } catch (caught) {
      submitError.capture(caught);
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <header className="border-b border-line-soft pb-5">
        <h1 className="page-title">{id ? "Редагування команди" : "Нова команда"}</h1>
        <p className="muted mt-2 text-body">Заповніть профіль команди.</p>
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
          Тег
          <input
            type="text"
            {...register("tag")}
            className="input"
          />
          {errors.tag && <p className="field-error">{errors.tag.message}</p>}
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
        <label className="field">
          Регіон
          <input
            type="text"
            {...register("region")}
            className="input"
          />
          {errors.region && <p className="field-error">{errors.region.message}</p>}
        </label>
        {!id && (
          <p className="rounded-lg border border-line bg-ink-800/60 px-3 py-2.5 text-micro text-text-muted">
            Капітаном стане поточний користувач — вказувати ID вручну не потрібно.
          </p>
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
            onClick={() => navigate("/teams")}
            className="btn btn-secondary"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default TeamForm;
