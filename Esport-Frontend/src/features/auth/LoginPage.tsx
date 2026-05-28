import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuthStore } from "../../store/authStore";
import { Link, useNavigate } from "react-router-dom";

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
    <div className="min-h-screen flex items-center justify-center bg-night-900 px-4">
      <div className="glass-panel w-full max-w-md rounded-2xl p-8">
        <h1 className="text-2xl font-semibold">Вхід до системи</h1>
        <p className="mt-2 text-sm text-slate-400">Поверніться до менеджера турнірів</p>
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
          <label className="block text-sm">
            Пароль
            <input
              type="password"
              {...register("password")}
              className="mt-2 w-full rounded-xl border border-white/10 bg-night-800/60 px-3 py-2 text-sm"
            />
            {errors.password && <p className="mt-1 text-xs text-neon-magenta">{errors.password.message}</p>}
          </label>
          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-xl bg-neon-cyan/90 px-4 py-2 text-sm font-semibold text-night-900 hover:bg-neon-cyan"
          >
            {isSubmitting ? "Вхід..." : "Увійти"}
          </button>
        </form>
        <p className="mt-4 text-sm text-slate-400">
          Немає акаунта? <Link to="/register" className="text-neon-cyan">Створити</Link>
        </p>
      </div>
    </div>
  );
};

export default LoginPage;
