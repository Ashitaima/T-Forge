import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuthStore } from "../../store/authStore";
import { Link, useNavigate } from "react-router-dom";

const schema = z.object({
  username: z
    .string()
    .min(3, "Вкажіть нікнейм")
    .regex(/^[a-zA-Z0-9_]+$/, "Нікнейм може містити лише літери, цифри та підкреслення"),
  firstName: z.string().min(2, "Вкажіть ім'я"),
  lastName: z.string().min(2, "Вкажіть прізвище"),
  email: z.string().email("Вкажіть коректну електронну пошту"),
  password: z
    .string()
    .min(8, "Мінімум 8 символів")
    .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/, "Пароль має містити великі/малі літери та цифру"),
  role: z.string().min(1, "Оберіть роль")
});

type FormValues = z.infer<typeof schema>;

const RegisterPage = () => {
  const navigate = useNavigate();
  const { register: registerUser } = useAuthStore();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      role: "User"
    }
  });

  const onSubmit = async (values: FormValues) => {
    try {
      setSubmitError(null);
      await registerUser(values);
      navigate("/");
    } catch (error) {
      const response =
        (error as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } })?.response
          ?.data;
      const validationErrors = response?.errors
        ? Object.values(response.errors).flat().join(" ")
        : null;
      const message =
        validationErrors ?? response?.message ?? "Не вдалося створити акаунт. Перевірте дані або спробуйте пізніше.";
      setSubmitError(message);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-night-900 px-4">
      <div className="glass-panel w-full max-w-md rounded-2xl p-8">
        <h1 className="text-2xl font-semibold">Реєстрація акаунта</h1>
        <p className="mt-2 text-sm text-slate-400">Створіть профіль менеджера</p>
        <form onSubmit={handleSubmit(onSubmit)} className="mt-6 space-y-4">
          <label className="block text-sm">
            Нікнейм
            <input
              type="text"
              {...register("username")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.username && <p className="mt-1 text-xs text-neon-magenta">{errors.username.message}</p>}
          </label>
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
          <label className="block text-sm">
            Електронна пошта
            <input
              type="email"
              {...register("email")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.email && <p className="mt-1 text-xs text-neon-magenta">{errors.email.message}</p>}
          </label>
          <label className="block text-sm">
            Пароль
            <input
              type="password"
              {...register("password")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.password && <p className="mt-1 text-xs text-neon-magenta">{errors.password.message}</p>}
          </label>
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
          {submitError && <p className="text-sm text-neon-magenta">{submitError}</p>}
          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-xl bg-neon-green/90 px-4 py-2 text-sm font-semibold text-night-900 hover:bg-neon-green"
          >
            {isSubmitting ? "Реєстрація..." : "Створити акаунт"}
          </button>
        </form>
        <p className="mt-4 text-sm text-slate-400">
          Вже є акаунт? <Link to="/login" className="text-neon-cyan">Увійти</Link>
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;
