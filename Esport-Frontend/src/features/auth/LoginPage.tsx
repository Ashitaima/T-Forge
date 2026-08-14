import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuthStore } from "../../store/authStore";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "./AuthLayout";

const schema = z.object({
  username: z.string().min(3, "Вкажіть нікнейм"),
  password: z.string().min(6, "Мінімум 6 символів")
});

type FormValues = z.infer<typeof schema>;

const LoginPage = () => {
  const navigate = useNavigate();
  const { login } = useAuthStore();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting }
  } = useForm<FormValues>({
    resolver: zodResolver(schema)
  });

  const onSubmit = async (values: FormValues) => {
    await login(values);
    navigate("/");
  };

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
      <form onSubmit={handleSubmit(onSubmit)} className="mt-7 space-y-4">
        <label className="field">
          Нікнейм
          <input type="text" autoComplete="username" {...register("username")} className="input" />
          {errors.username && <p className="field-error">{errors.username.message}</p>}
        </label>
        <label className="field">
          Пароль
          <input type="password" autoComplete="current-password" {...register("password")} className="input" />
          {errors.password && <p className="field-error">{errors.password.message}</p>}
        </label>
        <button type="submit" disabled={isSubmitting} className="btn btn-primary w-full">
          {isSubmitting ? "Вхід..." : "Увійти"}
        </button>
      </form>
    </AuthLayout>
  );
};

export default LoginPage;
