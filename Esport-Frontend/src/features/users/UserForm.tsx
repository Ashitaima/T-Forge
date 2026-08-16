import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { useSubmitError } from "../../hooks/useSubmitError";
import { usersApi } from "../../api/usersApi";

const schema = z.object({
  username: z.string().min(3, "Вкажіть нікнейм"),
  email: z.string().email("Вкажіть коректну пошту"),
  password: z.string().min(6, "Мінімум 6 символів").optional(),
  firstName: z.string().min(2, "Вкажіть ім'я"),
  lastName: z.string().min(2, "Вкажіть прізвище"),
  role: z.string().min(1, "Оберіть роль")
});

type FormValues = z.infer<typeof schema>;

const UserForm = () => {
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
        const data = await usersApi.getById(Number(id));
        setValue("username", data.username);
        setValue("email", data.email);
        setValue("firstName", data.firstName);
        setValue("lastName", data.lastName);
        setValue("role", data.role);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [id, setValue]);

  /** Сам запит. Помилку показує onSubmit нижче. */
  const save = async (values: FormValues) => {
    if (id) {
      await usersApi.update(Number(id), {
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email
      });
    } else {
      if (!values.password) {
        setError("password", { message: "Вкажіть пароль" });
        return;
      }
      await usersApi.create({
        username: values.username,
        email: values.email,
        password: values.password,
        firstName: values.firstName,
        lastName: values.lastName,
        role: values.role
      });
    }
  };

  const onSubmit = async (values: FormValues) => {
    submitError.clear();
    try {
      await save(values);
      navigate("/users");
    } catch (caught) {
      submitError.capture(caught);
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <header className="border-b border-line-soft pb-5">
        <h1 className="page-title">{id ? "Редагування користувача" : "Новий користувач"}</h1>
        <p className="muted mt-2 text-body">Керування обліковими даними та роллю.</p>
      </header>
      <form onSubmit={handleSubmit(onSubmit)} className="panel panel-body space-y-5">
        <label className="field">
          Нікнейм
          <input
            type="text"
            {...register("username")}
            className="input"
          />
          {errors.username && <p className="field-error">{errors.username.message}</p>}
        </label>
        <label className="field">
          Email
          <input
            type="email"
            {...register("email")}
            className="input"
          />
          {errors.email && <p className="field-error">{errors.email.message}</p>}
        </label>
        {!id && (
          <label className="field">
            Пароль
            <input
              type="password"
              {...register("password")}
              className="input"
            />
            {errors.password && <p className="field-error">{errors.password.message}</p>}
          </label>
        )}
        <div className="grid gap-4 md:grid-cols-2">
          <label className="field">
            Ім'я
            <input
              type="text"
              {...register("firstName")}
              className="input"
            />
            {errors.firstName && <p className="field-error">{errors.firstName.message}</p>}
          </label>
          <label className="field">
            Прізвище
            <input
              type="text"
              {...register("lastName")}
              className="input"
            />
            {errors.lastName && <p className="field-error">{errors.lastName.message}</p>}
          </label>
        </div>
        {!id && (
          <label className="field">
            Роль
            <select
              {...register("role")}
              className="input"
            >
              <option value="User">Користувач</option>
              <option value="Player">Гравець</option>
              <option value="Organizer">Організатор</option>
              <option value="Admin">Адміністратор</option>
            </select>
            {errors.role && <p className="field-error">{errors.role.message}</p>}
          </label>
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
            onClick={() => navigate("/users")}
            className="btn btn-secondary"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default UserForm;
