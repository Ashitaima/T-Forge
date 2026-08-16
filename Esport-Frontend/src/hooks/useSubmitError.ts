import { useState } from "react";
import type { FieldValues, Path, UseFormSetError } from "react-hook-form";

type ApiErrorBody = {
  message?: string;
  errors?: Record<string, string[]>;
};

const DEFAULT_MESSAGE = "Не вдалося зберегти. Перевірте дані або спробуйте пізніше.";

/**
 * Показ помилок, які повертає сервер.
 *
 * Валідація на клієнті ловить лише те, що видно формі: унікальність назви,
 * права доступу чи стан турніру перевіряє лише сервер. Без цього гачка така
 * відмова не показувала користувачеві геть нічого — форма просто лишалася
 * на місці.
 *
 * Бекенд віддає або {"message": "..."} з ExceptionMiddleware, або
 * {"errors": {"Поле": ["..."]}} від FluentValidation. Обробляємо обидва.
 */
export const useSubmitError = <T extends FieldValues>(setFieldError?: UseFormSetError<T>) => {
  const [error, setError] = useState<string | null>(null);

  const clear = () => setError(null);

  const capture = (caught: unknown, fallback = DEFAULT_MESSAGE) => {
    const body = (caught as { response?: { data?: ApiErrorBody } })?.response?.data;

    // Помилки полів кладемо ще й на самі поля — так видно, де саме проблема.
    if (body?.errors && setFieldError) {
      for (const [field, messages] of Object.entries(body.errors)) {
        const name = (field.charAt(0).toLowerCase() + field.slice(1)) as Path<T>;
        setFieldError(name, { message: messages.join(" ") });
      }
    }

    const flattened = body?.errors ? Object.values(body.errors).flat().join(" ") : null;
    setError(flattened ?? body?.message ?? fallback);
  };

  return { error, clear, capture };
};
