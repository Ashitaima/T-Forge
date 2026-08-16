import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft } from "lucide-react";
import { useAuthStore } from "../../store/authStore";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "./AuthLayout";
import { useSubmitError } from "../../hooks/useSubmitError";

const schema = z.object({
  username: z.string().min(3, "Вкажіть логін"),
  password: z.string().min(6, "Мінімум 6 символів")
});

type FormValues = z.infer<typeof schema>;

const LoginPage = () => {
  const navigate = useNavigate();
  const { login } = useAuthStore();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({
    resolver: zodResolver(schema)
  });

  const submitError = useSubmitError<FormValues>(setError);

  const onSubmit = async (values: FormValues) => {
    submitError.clear();
    try {
      await login(values);
      navigate("/");
    } catch (caught) {
      // Невідомий користувач і хибний пароль повертають різні повідомлення —
      // показуємо те, що сказав сервер, а не загальну фразу.
      submitError.capture(caught, "Не вдалося увійти. Перевірте логін і пароль.");
    }
  };

  const goBack = () => (window.history.length > 1 ? navigate(-1) : navigate("/"));

  return (
    <AuthLayout
      title="Вхід"
      subtitle="Продовжуйте керувати своїми турнірами."
      footer={
        <>
          Немає акаунта?{" "}
          <Link to="/register" className="text-ember hover:underline">
            Створити
          </Link>
        </>
      }
    >
      <button type="button" onClick={goBack} className="btn btn-ghost btn-sm -ml-2 mt-5">
        <ArrowLeft className="h-4 w-4" />
        Назад
      </button>

      <form onSubmit={handleSubmit(onSubmit)} className="mt-4 space-y-4">
        <label className="field">
          Логін
          <input type="text" autoComplete="username" {...register("username")} className="input" />
          {errors.username && <p className="field-error">{errors.username.message}</p>}
        </label>
        <label className="field">
          Пароль
          <input type="password" autoComplete="current-password" {...register("password")} className="input" />
          {errors.password && <p className="field-error">{errors.password.message}</p>}
        </label>
        {submitError.error && <div className="notice notice-error">{submitError.error}</div>}
        <button type="submit" disabled={isSubmitting} className="btn btn-primary w-full">
          {isSubmitting ? "Вхід..." : "Увійти"}
        </button>
      </form>
    </AuthLayout>
  );
};

export default LoginPage;
