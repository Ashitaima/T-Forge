import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
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

  const onSubmit = async (values: FormValues) => {
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

    navigate("/users");
  };

  return (
    <div className="max-w-2xl space-y-6">
      <header>
        <h1 className="text-3xl font-semibold">{id ? "Редагування користувача" : "Новий користувач"}</h1>
        <p className="mt-2 text-sm text-slate-400">Керування обліковими даними та роллю.</p>
      </header>
      <form onSubmit={handleSubmit(onSubmit)} className="glass-panel rounded-2xl p-6 space-y-4">
        <label className="block text-sm">
          Нікнейм
          <input
            type="text"
            {...register("username")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.username && <p className="mt-1 text-xs text-neon-magenta">{errors.username.message}</p>}
        </label>
        <label className="block text-sm">
          Email
          <input
            type="email"
            {...register("email")}
            className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
          />
          {errors.email && <p className="mt-1 text-xs text-neon-magenta">{errors.email.message}</p>}
        </label>
        {!id && (
          <label className="block text-sm">
            Пароль
            <input
              type="password"
              {...register("password")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.password && <p className="mt-1 text-xs text-neon-magenta">{errors.password.message}</p>}
          </label>
        )}
        <div className="grid gap-4 md:grid-cols-2">
          <label className="block text-sm">
            Ім'я
            <input
              type="text"
              {...register("firstName")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.firstName && <p className="mt-1 text-xs text-neon-magenta">{errors.firstName.message}</p>}
          </label>
          <label className="block text-sm">
            Прізвище
            <input
              type="text"
              {...register("lastName")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.lastName && <p className="mt-1 text-xs text-neon-magenta">{errors.lastName.message}</p>}
          </label>
        </div>
        {!id && (
          <label className="block text-sm">
            Роль
            <select
              {...register("role")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            >
              <option value="User">Користувач</option>
              <option value="Player">Гравець</option>
              <option value="Organizer">Організатор</option>
              <option value="Admin">Адміністратор</option>
            </select>
            {errors.role && <p className="mt-1 text-xs text-neon-magenta">{errors.role.message}</p>}
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
            onClick={() => navigate("/users")}
            className="rounded-xl border border-white/10 px-4 py-2 text-sm text-slate-300"
          >
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default UserForm;
