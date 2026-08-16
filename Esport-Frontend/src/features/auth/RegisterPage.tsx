import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuthStore } from "../../store/authStore";
import { ArrowLeft } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "./AuthLayout";
import { NICKNAME_MAX_LENGTH, NICKNAME_MIN_LENGTH, NICKNAME_PATTERN } from "../../constants/playerPositions";

const schema = z
  .object({
    username: z
      .string()
      .min(3, "Вкажіть логін")
      .regex(/^[a-zA-Z0-9_]+$/, "Логін може містити лише літери, цифри та підкреслення"),
    firstName: z.string().min(2, "Вкажіть ім'я"),
    lastName: z.string().min(2, "Вкажіть прізвище"),
    email: z.string().email("Вкажіть коректну електронну пошту"),
    password: z
      .string()
      .min(8, "Мінімум 8 символів")
      .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/, "Пароль має містити великі/малі літери та цифру"),
    role: z.enum(["Player", "Organizer"], { required_error: "Оберіть роль" }),
    nickname: z.string().optional()
  })
  // Профіль гравця створюється одразу під час реєстрації — без нікнейма
  // його нема з чого зробити. Організаторові профіль не створюється.
  .superRefine((values, ctx) => {
    if (values.role !== "Player") {
      return;
    }

    const nickname = values.nickname?.trim() ?? "";

    if (nickname.length < NICKNAME_MIN_LENGTH || nickname.length > NICKNAME_MAX_LENGTH) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["nickname"],
        message: `Ігровий нікнейм: від ${NICKNAME_MIN_LENGTH} до ${NICKNAME_MAX_LENGTH} символів`
      });
      return;
    }

    if (!NICKNAME_PATTERN.test(nickname)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["nickname"],
        message: "Нікнейм може містити лише літери, цифри та підкреслення"
      });
    }
  });

type FormValues = z.infer<typeof schema>;

const RegisterPage = () => {
  const navigate = useNavigate();
  const { register: registerUser } = useAuthStore();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      role: "Player",
      nickname: ""
    }
  });

  const role = watch("role");

  const onSubmit = async (values: FormValues) => {
    try {
      setSubmitError(null);
      await registerUser({
        username: values.username,
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        password: values.password,
        role: values.role,
        nickname: values.role === "Player" ? (values.nickname?.trim() ?? "") : ""
      });
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
    <AuthLayout
      title="Створення акаунта"
      subtitle="Кілька полів — і можна збирати команду."
      footer={
        <>
          Вже є акаунт?{" "}
          <Link to="/login" className="text-ember hover:underline">
            Увійти
          </Link>
        </>
      }
    >
      <button
        type="button"
        onClick={() => (window.history.length > 1 ? navigate(-1) : navigate("/"))}
        className="btn btn-ghost btn-sm -ml-2 mt-5"
      >
        <ArrowLeft className="h-4 w-4" />
        Назад
      </button>

      <form onSubmit={handleSubmit(onSubmit)} className="mt-4 space-y-4">
        <label className="field">
          Логін
          <input type="text" autoComplete="username" {...register("username")} className="input" />
          {errors.username ? (
            <p className="field-error">{errors.username.message}</p>
          ) : (
            <p className="field-hint">За ним ви входитимете в систему.</p>
          )}
        </label>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="field">
            Ім&#39;я
            <input type="text" autoComplete="given-name" {...register("firstName")} className="input" />
            {errors.firstName && <p className="field-error">{errors.firstName.message}</p>}
          </label>
          <label className="field">
            Прізвище
            <input type="text" autoComplete="family-name" {...register("lastName")} className="input" />
            {errors.lastName && <p className="field-error">{errors.lastName.message}</p>}
          </label>
        </div>
        <label className="field">
          Електронна пошта
          <input type="email" autoComplete="email" {...register("email")} className="input" />
          {errors.email && <p className="field-error">{errors.email.message}</p>}
        </label>
        <label className="field">
          Пароль
          <input type="password" autoComplete="new-password" {...register("password")} className="input" />
          {errors.password ? (
            <p className="field-error">{errors.password.message}</p>
          ) : (
            <p className="field-hint">Мінімум 8 символів, велика й мала літери та цифра.</p>
          )}
        </label>
        <label className="field">
          Роль
          <select {...register("role")} className="input">
            <option value="Player">Гравець</option>
            <option value="Organizer">Організатор</option>
          </select>
          {errors.role ? (
            <p className="field-error">{errors.role.message}</p>
          ) : (
            <p className="field-hint">Гравець виступає за команду. Організатор проводить турніри.</p>
          )}
        </label>
        {role === "Player" && (
          <label className="field">
            Ігровий нікнейм
            <input type="text" {...register("nickname")} className="input" />
            {errors.nickname ? (
              <p className="field-error">{errors.nickname.message}</p>
            ) : (
              <p className="field-hint">Під цим імʼям вас бачитимуть у складах і таблицях.</p>
            )}
          </label>
        )}
        {submitError && <div className="notice notice-error">{submitError}</div>}
        <button type="submit" disabled={isSubmitting} className="btn btn-primary w-full">
          {isSubmitting ? "Створення..." : "Створити акаунт"}
        </button>
      </form>
    </AuthLayout>
  );
};

export default RegisterPage;
