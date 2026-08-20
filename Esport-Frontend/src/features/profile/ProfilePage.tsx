import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { authApi } from "../../api/authApi";
import { playersApi } from "../../api/playersApi";
import { COUNTRY_NAMES } from "../../constants/countries";
import { GAME_ID_FIELDS, emptyToUndefined, isValidGameId } from "../../constants/gameIds";
import { usersApi } from "../../api/usersApi";
import { useAuthStore } from "../../store/authStore";
import { PageHeader, Skeleton } from "../../components/ui/Primitives";
import { Avatar } from "../../components/ui/Avatar";
import { GameProfilesPanel } from "./GameProfilesPanel";
import { OrganizerRequestPanel } from "./OrganizerRequestPanel";
import {
  NICKNAME_MAX_LENGTH,
  NICKNAME_MIN_LENGTH,
  NICKNAME_PATTERN,
  PLAYER_MAX_AGE,
  PLAYER_MIN_AGE
} from "../../constants/playerPositions";
import type { PlayerDto } from "../../types";

const accountSchema = z.object({
  // Справжнє ім'я необов'язкове — усюди в системі видно нікнейм.
  firstName: z.string().max(50, "Максимум 50 символів"),
  lastName: z.string().max(50, "Максимум 50 символів"),
  email: z.string().email("Вкажіть коректну електронну пошту"),
  isNameHidden: z.boolean()
});

const playerSchema = z.object({
  nickname: z
    .string()
    .min(NICKNAME_MIN_LENGTH, `Мінімум ${NICKNAME_MIN_LENGTH} символи`)
    .max(NICKNAME_MAX_LENGTH, `Максимум ${NICKNAME_MAX_LENGTH} символів`)
    .regex(NICKNAME_PATTERN, "Лише літери, цифри та підкреслення"),
  // Позиції тут немає: роль залежить від дисципліни, тож її задають у блоці
  // «Дисципліни» окремо для кожної гри (див. GameProfilesPanel).
  // Країна й вік необов'язкові — порожнє значення означає «не вказав».
  country: z
    .string()
    .refine((code) => code === "" || code in COUNTRY_NAMES, { message: "Оберіть країну зі списку" }),
  age: z.coerce
    .number()
    .refine((value) => value === 0 || (value >= PLAYER_MIN_AGE && value <= PLAYER_MAX_AGE), {
      message: `Вік — від ${PLAYER_MIN_AGE} до ${PLAYER_MAX_AGE} років`
    }),
  // Теги мусять їхати разом з рештою: UpdatePlayerDto перезаписує їх цілком,
  // тож форма без цих полів мовчки стирала б уже збережені.
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
  // Приватність їде разом з рештою: UpdatePlayerDto перезаписує профіль
  // цілком, тож форма без цих полів скидала б збережені перемикачі.
  isAgeHidden: z.boolean(),
  isCountryHidden: z.boolean()
});

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, "Вкажіть поточний пароль"),
    newPassword: z
      .string()
      .min(8, "Мінімум 8 символів")
      .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/, "Пароль має містити великі/малі літери та цифру"),
    confirmPassword: z.string()
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ["confirmPassword"],
    message: "Паролі не збігаються"
  });

type AccountValues = z.infer<typeof accountSchema>;
type PlayerValues = z.infer<typeof playerSchema>;
type PasswordValues = z.infer<typeof passwordSchema>;
type Notice = { kind: "ok" | "error"; text: string } | null;

/** Дістає читабельне повідомлення з відповіді API. */
const readApiError = (error: unknown, fallback: string) => {
  const response = (error as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } })
    ?.response?.data;
  const validationErrors = response?.errors ? Object.values(response.errors).flat().join(" ") : null;
  return validationErrors ?? response?.message ?? fallback;
};

const NoticeBox = ({ notice }: { notice: Notice }) =>
  notice ? (
    <div className={notice.kind === "ok" ? "notice notice-success" : "notice notice-error"}>
      {notice.text}
    </div>
  ) : null;

const ProfilePage = () => {
  const { user, setUser } = useAuthStore();
  const [player, setPlayer] = useState<PlayerDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [accountNotice, setAccountNotice] = useState<Notice>(null);
  const [playerNotice, setPlayerNotice] = useState<Notice>(null);
  const [passwordNotice, setPasswordNotice] = useState<Notice>(null);
  const [avatarNotice, setAvatarNotice] = useState<Notice>(null);
  const [avatarBusy, setAvatarBusy] = useState(false);

  const initials =
    `${user?.firstName?.[0] ?? ""}${user?.lastName?.[0] ?? ""}`.trim() || user?.username?.[0] || "?";

  // Ті самі межі, що й на сервері (Common/AvatarRules.cs) — щоб користувач
  // дізнався про завеликий файл до вивантаження.
  const MAX_AVATAR_BYTES = 2 * 1024 * 1024;
  const ALLOWED_AVATAR_TYPES = ["image/jpeg", "image/png", "image/webp"];

  const uploadAvatar = async (file: File) => {
    setAvatarNotice(null);

    if (!ALLOWED_AVATAR_TYPES.includes(file.type)) {
      setAvatarNotice({ kind: "error", text: "Підтримуються лише JPEG, PNG і WebP." });
      return;
    }
    if (file.size > MAX_AVATAR_BYTES) {
      setAvatarNotice({ kind: "error", text: "Файл завеликий: максимум 2 МБ." });
      return;
    }
    if (!user) {
      return;
    }

    setAvatarBusy(true);
    try {
      setUser(await usersApi.uploadAvatar(user.id, file));
      setAvatarNotice({ kind: "ok", text: "Аватар оновлено." });
    } catch (error) {
      setAvatarNotice({ kind: "error", text: readApiError(error, "Не вдалося завантажити аватар.") });
    } finally {
      setAvatarBusy(false);
    }
  };

  const removeAvatar = async () => {
    if (!user) {
      return;
    }
    setAvatarBusy(true);
    setAvatarNotice(null);
    try {
      await usersApi.deleteAvatar(user.id);
      setUser({ ...user, avatarUrl: null });
      setAvatarNotice({ kind: "ok", text: "Аватар прибрано." });
    } catch (error) {
      setAvatarNotice({ kind: "error", text: readApiError(error, "Не вдалося прибрати аватар.") });
    } finally {
      setAvatarBusy(false);
    }
  };

  const accountForm = useForm<AccountValues>({ resolver: zodResolver(accountSchema) });
  const playerForm = useForm<PlayerValues>({ resolver: zodResolver(playerSchema) });
  const passwordForm = useForm<PasswordValues>({ resolver: zodResolver(passwordSchema) });

  const { reset: resetAccount } = accountForm;
  const { reset: resetPlayer } = playerForm;

  useEffect(() => {
    if (!user) {
      return;
    }

    resetAccount({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      isNameHidden: user.isNameHidden ?? false
    });
  }, [user, resetAccount]);

  useEffect(() => {
    let isActive = true;

    // Профіль гравця є не в кожного — організатор і адміністратор його не мають.
    playersApi
      .getMe()
      .then((data) => {
        if (!isActive) {
          return;
        }
        setPlayer(data);
        resetPlayer({
          nickname: data.nickname,
          country: data.country,
          age: data.age,
          riotId: data.riotId ?? "",
          steamId64: data.steamId64 ?? "",
          battleTag: data.battleTag ?? "",
          isAgeHidden: data.isAgeHidden ?? false,
          isCountryHidden: data.isCountryHidden ?? false
        });
      })
      .catch(() => isActive && setPlayer(null))
      .finally(() => isActive && setLoading(false));

    return () => {
      isActive = false;
    };
  }, [resetPlayer]);

  const submitAccount = async (values: AccountValues) => {
    try {
      const updated = await authApi.updateProfile(values);
      setUser(updated);
      setAccountNotice({ kind: "ok", text: "Дані збережено." });
    } catch (error) {
      setAccountNotice({ kind: "error", text: readApiError(error, "Не вдалося зберегти дані.") });
    }
  };

  const submitPlayer = async (values: PlayerValues) => {
    if (!player) {
      return;
    }

    try {
      const updated = await playersApi.update(player.id, {
        ...values,
        riotId: emptyToUndefined(values.riotId),
        steamId64: emptyToUndefined(values.steamId64),
        battleTag: emptyToUndefined(values.battleTag)
      });
      setPlayer(updated);
      setPlayerNotice({ kind: "ok", text: "Профіль гравця збережено." });
    } catch (error) {
      setPlayerNotice({ kind: "error", text: readApiError(error, "Не вдалося зберегти профіль гравця.") });
    }
  };

  const submitPassword = async (values: PasswordValues) => {
    try {
      await authApi.changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword
      });
      passwordForm.reset({ currentPassword: "", newPassword: "", confirmPassword: "" });
      setPasswordNotice({ kind: "ok", text: "Пароль змінено." });
    } catch (error) {
      setPasswordNotice({ kind: "error", text: readApiError(error, "Не вдалося змінити пароль.") });
    }
  };

  return (
    <>
      <PageHeader
        eyebrow="Акаунт"
        title="Мій профіль"
        description="Особисті дані, ігровий профіль і пароль."
      />

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Аватар</h2>
        </div>
        <div className="panel-body space-y-4">
          <div className="flex flex-wrap items-center gap-5">
            <Avatar url={user?.avatarUrl} fallback={initials} size="lg" alt="Ваш аватар" />
            <div className="space-y-2">
              <label className="btn btn-secondary btn-sm cursor-pointer">
                {avatarBusy ? "Завантаження..." : "Вибрати файл"}
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  className="hidden"
                  disabled={avatarBusy}
                  onChange={(event) => {
                    const file = event.target.files?.[0];
                    if (file) {
                      uploadAvatar(file);
                    }
                    // Скидаємо значення, щоб повторний вибір того самого файлу
                    // теж викликав onChange.
                    event.target.value = "";
                  }}
                />
              </label>
              {user?.avatarUrl && (
                <button
                  type="button"
                  onClick={removeAvatar}
                  disabled={avatarBusy}
                  className="btn btn-ghost btn-sm"
                >
                  Прибрати
                </button>
              )}
              <p className="field-hint">JPEG, PNG або WebP, до 2 МБ.</p>
            </div>
          </div>
          <NoticeBox notice={avatarNotice} />
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Обліковий запис</h2>
        </div>
        <form onSubmit={accountForm.handleSubmit(submitAccount)} className="panel-body space-y-5">
          <label className="field">
            Логін
            <input type="text" value={user?.username ?? ""} readOnly disabled className="input" />
            <p className="field-hint">Логін змінити не можна — за ним ви входите в систему.</p>
          </label>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="field">
              Ім&#39;я <span className="text-micro font-normal text-text-faint">— необов&#39;язково</span>
              <input type="text" {...accountForm.register("firstName")} className="input" />
              {accountForm.formState.errors.firstName && (
                <p className="field-error">{accountForm.formState.errors.firstName.message}</p>
              )}
            </label>
            <label className="field">
              Прізвище <span className="text-micro font-normal text-text-faint">— необов&#39;язково</span>
              <input type="text" {...accountForm.register("lastName")} className="input" />
              {accountForm.formState.errors.lastName && (
                <p className="field-error">{accountForm.formState.errors.lastName.message}</p>
              )}
            </label>
          </div>
          <label className="field">
            Електронна пошта
            <input type="email" {...accountForm.register("email")} className="input" />
            {accountForm.formState.errors.email && (
              <p className="field-error">{accountForm.formState.errors.email.message}</p>
            )}
          </label>
          <label className="flex items-start gap-2.5">
            <input
              type="checkbox"
              {...accountForm.register("isNameHidden")}
              className="mt-0.5 h-4 w-4 shrink-0"
            />
            <span className="text-body text-text">
              Приховати справжнє імʼя
              <span className="field-hint mt-0.5 block">
                Інші бачитимуть лише нікнейм. Пошта не показується нікому.
              </span>
            </span>
          </label>
          <NoticeBox notice={accountNotice} />
          <div className="border-t border-line-soft pt-5">
            <button type="submit" disabled={accountForm.formState.isSubmitting} className="btn btn-primary">
              {accountForm.formState.isSubmitting ? "Збереження..." : "Зберегти"}
            </button>
          </div>
        </form>
      </section>

      {loading && <Skeleton rows={3} />}

      {!loading && player && (
        <section className="panel">
          <div className="panel-header">
            <h2 className="section-title">Профіль гравця</h2>
          </div>
          <form onSubmit={playerForm.handleSubmit(submitPlayer)} className="panel-body space-y-5">
            <label className="field">
              Нікнейм
              <input type="text" {...playerForm.register("nickname")} className="input" />
              {playerForm.formState.errors.nickname && (
                <p className="field-error">{playerForm.formState.errors.nickname.message}</p>
              )}
            </label>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="field">
                Країна <span className="text-micro font-normal text-text-faint">— необов&#39;язково</span>
                <select {...playerForm.register("country")} className="input" defaultValue="">
                  <option value="">Не вказано</option>
                  {Object.entries(COUNTRY_NAMES).map(([code, name]) => (
                    <option key={code} value={code}>
                      {name}
                    </option>
                  ))}
                </select>
                {playerForm.formState.errors.country && (
                  <p className="field-error">{playerForm.formState.errors.country.message}</p>
                )}
              </label>
            </div>
            <label className="field">
              Вік <span className="text-micro font-normal text-text-faint">— необов&#39;язково</span>
              <input type="number" {...playerForm.register("age")} className="input" />
              {playerForm.formState.errors.age && (
                <p className="field-error">{playerForm.formState.errors.age.message}</p>
              )}
            </label>

            {/* Приховати можна лише те, від чого не залежать таблиці: нікнейм,
                показники й рейтинг лишаються видимими завжди. */}
            <fieldset className="space-y-2.5 border-t border-line-soft pt-5">
              <legend className="eyebrow">Приватність</legend>
              <label className="flex items-center gap-2.5">
                <input
                  type="checkbox"
                  {...playerForm.register("isCountryHidden")}
                  className="h-4 w-4 shrink-0"
                />
                <span className="text-body text-text">Приховати країну</span>
              </label>
              <label className="flex items-center gap-2.5">
                <input
                  type="checkbox"
                  {...playerForm.register("isAgeHidden")}
                  className="h-4 w-4 shrink-0"
                />
                <span className="text-body text-text">Приховати вік</span>
              </label>
              <p className="field-hint">
                Нікнейм, показники й рейтинг видно завжди — на них тримаються таблиці.
              </p>
            </fieldset>

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
                    {...playerForm.register(tag.name)}
                    className="input font-mono"
                  />
                  {playerForm.formState.errors[tag.name] && (
                    <p className="field-error">
                      {playerForm.formState.errors[tag.name]?.message}
                    </p>
                  )}
                </label>
              ))}
            </fieldset>
            <NoticeBox notice={playerNotice} />
            <div className="border-t border-line-soft pt-5">
              <button type="submit" disabled={playerForm.formState.isSubmitting} className="btn btn-primary">
                {playerForm.formState.isSubmitting ? "Збереження..." : "Зберегти"}
              </button>
            </div>
          </form>
        </section>
      )}

      {!loading && player && (
        <GameProfilesPanel
          playerId={player.id}
          profiles={player.gameProfiles ?? []}
          onChange={(gameProfiles) => setPlayer({ ...player, gameProfiles })}
        />
      )}

      <section className="panel">
        <div className="panel-header">
          <h2 className="section-title">Пароль</h2>
        </div>
        <form onSubmit={passwordForm.handleSubmit(submitPassword)} className="panel-body space-y-5">
          <label className="field">
            Поточний пароль
            <input
              type="password"
              autoComplete="current-password"
              {...passwordForm.register("currentPassword")}
              className="input"
            />
            {passwordForm.formState.errors.currentPassword && (
              <p className="field-error">{passwordForm.formState.errors.currentPassword.message}</p>
            )}
          </label>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="field">
              Новий пароль
              <input
                type="password"
                autoComplete="new-password"
                {...passwordForm.register("newPassword")}
                className="input"
              />
              {passwordForm.formState.errors.newPassword && (
                <p className="field-error">{passwordForm.formState.errors.newPassword.message}</p>
              )}
            </label>
            <label className="field">
              Повторіть пароль
              <input
                type="password"
                autoComplete="new-password"
                {...passwordForm.register("confirmPassword")}
                className="input"
              />
              {passwordForm.formState.errors.confirmPassword && (
                <p className="field-error">{passwordForm.formState.errors.confirmPassword.message}</p>
              )}
            </label>
          </div>
          <NoticeBox notice={passwordNotice} />
          <div className="border-t border-line-soft pt-5">
            <button type="submit" disabled={passwordForm.formState.isSubmitting} className="btn btn-primary">
              {passwordForm.formState.isSubmitting ? "Збереження..." : "Змінити пароль"}
            </button>
          </div>
        </form>
      </section>

      {/* Найнижче й згорнуто: ролі просить меншість, тож блок не має тіснити
          те, чим користуються всі. Роль читаємо справжню, а не переглядову —
          заявку подають від власного акаунта. */}
      {user && <OrganizerRequestPanel role={user.role} />}
    </>
  );
};

export default ProfilePage;
