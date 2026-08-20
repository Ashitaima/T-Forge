import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, useParams } from "react-router-dom";
import { playersApi } from "../../api/playersApi";
import { COUNTRY_CODES, COUNTRY_NAMES, countryFlag } from "../../constants/countries";
import {
  NICKNAME_MAX_LENGTH,
  NICKNAME_MIN_LENGTH,
  NICKNAME_PATTERN,
  PLAYER_MAX_AGE,
  PLAYER_MIN_AGE
} from "../../constants/playerPositions";
import { GAME_ID_FIELDS, emptyToUndefined, isValidGameId } from "../../constants/gameIds";
import type { UpdatePlayerDto } from "../../types";

const schema = z.object({
  nickname: z
    .string()
    .min(NICKNAME_MIN_LENGTH, `Мінімум ${NICKNAME_MIN_LENGTH} символи`)
    .max(NICKNAME_MAX_LENGTH, `Максимум ${NICKNAME_MAX_LENGTH} символів`)
    .regex(NICKNAME_PATTERN, "Лише літери, цифри та підкреслення"),
  // Позиція, країна й вік необов'язкові — порожнє значення означає
  // «не вказав». Країна зберігається кодом ISO (з нього виводиться прапор);
  // перелік мусить збігатися з Esport-Backend/Common/Countries.cs.
  country: z
    .string()
    .refine((code) => code === "" || code in COUNTRY_NAMES, { message: "Оберіть країну зі списку" }),
  age: z.coerce
    .number()
    .refine((value) => value === 0 || (value >= PLAYER_MIN_AGE && value <= PLAYER_MAX_AGE), {
      message: `Вік — від ${PLAYER_MIN_AGE} до ${PLAYER_MAX_AGE} років`
    }),
  // Ігрові теги необов'язкові, але заповнений має бути правильним:
  // за поламаним тегом гравця однаково ніхто не знайде.
  // Формати — дзеркало Esport-Backend/Common/GameIdFormats.cs.
  riotId: z
    .string()
    .optional()
    .refine((value) => isValidGameId(GAME_ID_FIELDS[0], value), {
      message: GAME_ID_FIELDS[0].error
    }),
  steamId64: z
    .string()
    .optional()
    .refine((value) => isValidGameId(GAME_ID_FIELDS[1], value), {
      message: GAME_ID_FIELDS[1].error
    }),
  battleTag: z
    .string()
    .optional()
    .refine((value) => isValidGameId(GAME_ID_FIELDS[2], value), {
      message: GAME_ID_FIELDS[2].error
    }),
  username: z.string().optional(),
  email: z.string().optional(),
  password: z.string().optional()
});

type FormValues = z.infer<typeof schema>;

/** Дістає читабельне повідомлення з відповіді API. */
const readApiError = (error: unknown, fallback: string) => {
  const response = (error as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } })
    ?.response?.data;
  const validationErrors = response?.errors ? Object.values(response.errors).flat().join(" ") : null;
  return validationErrors ?? response?.message ?? fallback;
};

const PlayerForm = () => {
  const navigate = useNavigate();
  const { id } = useParams();
  const [loading, setLoading] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  // Приватність налаштовує сам гравець у себе в профілі, а не адміністратор
  // тут. Але UpdatePlayerDto перезаписує профіль цілком, тож збережені
  // перемикачі треба провести крізь форму, інакше вона їх мовчки скине.
  const [privacy, setPrivacy] = useState({ isAgeHidden: false, isCountryHidden: false });

  // Створення профілю доступне лише адміністраторові (маршрут закритий роллю),
  // і воно завжди створює повний обліковий запис — звичайний користувач
  // отримує профіль автоматично під час реєстрації.
  const isCreatingAccount = !id;

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
        const data = await playersApi.getById(Number(id));
        setValue("nickname", data.nickname);
        setValue("country", data.country);
        setValue("age", data.age);
        setValue("riotId", data.riotId ?? "");
        setValue("steamId64", data.steamId64 ?? "");
        setValue("battleTag", data.battleTag ?? "");
        setPrivacy({
          isAgeHidden: data.isAgeHidden ?? false,
          isCountryHidden: data.isCountryHidden ?? false
        });
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [id, setValue]);

  const onSubmit = async (values: FormValues) => {
    setSubmitError(null);

    try {
      if (isCreatingAccount) {
        const username = values.username?.trim() ?? "";
        const email = values.email?.trim() ?? "";
        const password = values.password ?? "";

        if (username.length < 3 || !NICKNAME_PATTERN.test(username)) {
          setError("username", { message: "Лише літери, цифри та підкреслення, від 3 символів" });
          return;
        }
        if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) {
          setError("email", { message: "Вкажіть коректну електронну пошту" });
          return;
        }
        if (password.length < 8 || !/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/.test(password)) {
          setError("password", { message: "Мінімум 8 символів, великі/малі літери та цифра" });
          return;
        }

        await playersApi.createFull({
          username,
          email,
          password,
          // Справжнє ім'я необов'язкове: власник акаунта заповнить його сам,
          // якщо схоче.
          firstName: "",
          lastName: "",
          nickname: values.nickname,
          // Позицію задають подисциплінно вже в самому профілі.
          position: "",
          country: values.country,
          age: values.age
        });
      } else {
        const payload: UpdatePlayerDto = {
          nickname: values.nickname,
          country: values.country,
          age: values.age,
          riotId: emptyToUndefined(values.riotId),
          steamId64: emptyToUndefined(values.steamId64),
          battleTag: emptyToUndefined(values.battleTag),
          ...privacy
        };
        await playersApi.update(Number(id), payload);
      }

      navigate("/players");
    } catch (error) {
      setSubmitError(readApiError(error, "Не вдалося зберегти. Перевірте дані або спробуйте пізніше."));
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <header className="border-b border-line-soft pb-5">
        <h1 className="page-title">{id ? "Редагування гравця" : "Новий акаунт гравця"}</h1>
        <p className="muted mt-2 text-body">
          {id ? "Оновіть персональні дані гравця." : "Буде створено обліковий запис і профіль гравця."}
        </p>
      </header>
      <form onSubmit={handleSubmit(onSubmit)} className="panel panel-body space-y-5">
        {isCreatingAccount && (
          <>
            <label className="field">
              Логін
              <input type="text" autoComplete="off" {...register("username")} className="input" />
              {errors.username && <p className="field-error">{errors.username.message}</p>}
            </label>
            <label className="field">
              Електронна пошта
              <input type="email" autoComplete="off" {...register("email")} className="input" />
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
          </>
        )}
        <label className="field">
          Нікнейм
          <input type="text" {...register("nickname")} className="input" />
          {errors.nickname && <p className="field-error">{errors.nickname.message}</p>}
        </label>
        <label className="field">
          Країна <span className="text-micro font-normal text-text-faint">— необов&#39;язково</span>
          <select {...register("country")} className="input" defaultValue="">
            <option value="">Не вказано</option>
            {COUNTRY_CODES.map((code) => (
              <option key={code} value={code}>
                {countryFlag(code)} {COUNTRY_NAMES[code]}
              </option>
            ))}
          </select>
          {errors.country && <p className="field-error">{errors.country.message}</p>}
        </label>
        <label className="field">
          Вік <span className="text-micro font-normal text-text-faint">— необов&#39;язково</span>
          <input type="number" {...register("age")} className="input" />
          {errors.age && <p className="field-error">{errors.age.message}</p>}
        </label>
        <fieldset className="space-y-4 border-t border-line-soft pt-5">
          <legend className="eyebrow">Ігрові акаунти</legend>
          <p className="text-micro text-text-faint">
            Необов'язково. Потрібні, щоб суперник знайшов вас у грі.
          </p>

          {GAME_ID_FIELDS.map((tag) => (
            <label key={tag.name} className="field">
              {tag.label}
              <span className="ml-2 text-micro font-normal text-text-faint">{tag.hint}</span>
              <input
                type="text"
                autoComplete="off"
                spellCheck={false}
                placeholder={tag.placeholder}
                {...register(tag.name)}
                className="input font-mono"
              />
              {errors[tag.name] && <p className="field-error">{errors[tag.name]?.message}</p>}
            </label>
          ))}
        </fieldset>

        {isCreatingAccount && (
          <p className="rounded-lg border border-line bg-ink-800/60 px-3 py-2.5 text-micro text-text-muted">
            Буде створено обліковий запис із роллю «Гравець» і привʼязаний до нього профіль.
          </p>
        )}
        {submitError && <div className="notice notice-error">{submitError}</div>}
        {loading && <div className="text-micro text-text-faint">Завантаження даних...</div>}
        <div className="flex items-center gap-3 border-t border-line-soft pt-5">
          <button type="submit" disabled={isSubmitting} className="btn btn-primary">
            {isSubmitting ? "Збереження..." : "Зберегти"}
          </button>
          <button type="button" onClick={() => navigate("/players")} className="btn btn-secondary">
            Скасувати
          </button>
        </div>
      </form>
    </div>
  );
};

export default PlayerForm;
