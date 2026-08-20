import { useCallback, useEffect, useState } from "react";
import { organizerRequestsApi } from "../../api/organizerRequestsApi";
import { useSubmitError } from "../../hooks/useSubmitError";
import type { OrganizerRequestDto } from "../../types";

const formatDate = (value: string) =>
  new Date(value).toLocaleDateString("uk-UA", { day: "2-digit", month: "short", year: "numeric" });

/**
 * Заявка на роль організатора.
 *
 * Роль дає право створювати турніри, тож її не можна просто обрати — заявку
 * розглядає адміністратор (див. Common/OrganizerRequestPolicy.cs). Панель
 * має сенс лише для гравця: організатор і адміністратор це право вже мають.
 */
export const OrganizerRequestPanel = ({ role }: { role: string }) => {
  const [requests, setRequests] = useState<OrganizerRequestDto[]>([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const submitError = useSubmitError();

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setRequests(await organizerRequestsApi.getMine());
    } catch {
      setRequests([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  if (role !== "Player") {
    return null;
  }

  const pending = requests.find((request) => request.status === "Pending");
  const lastDeclined = requests.find((request) => request.status === "Declined");

  const run = async (action: () => Promise<unknown>) => {
    submitError.clear();
    try {
      await action();
      await load();
    } catch (error) {
      submitError.capture(error);
    }
  };

  return (
    // Згорнута за замовчуванням: більшість гравців ролі не просить, і
    // розгорнутий блок займав би місце в кожного.
    <details className="panel group">
      <summary className="panel-header cursor-pointer list-none">
        <h2 className="section-title">Роль організатора</h2>
        <span className="muted text-micro">
          {pending ? "Заявка на розгляді" : "Розгорнути"}
        </span>
      </summary>
      <div className="panel-body space-y-4">
        {loading && <p className="muted text-micro">Завантаження...</p>}

        {!loading && pending && (
          <>
            <div className="notice">
              Заявку подано {formatDate(pending.createdAt)} — вона чекає на розгляд адміністратора.
            </div>
            <button
              type="button"
              onClick={() => run(() => organizerRequestsApi.cancel(pending.id))}
              className="btn btn-ghost btn-sm"
            >
              Відкликати заявку
            </button>
          </>
        )}

        {!loading && !pending && (
          <>
            <p className="text-micro text-text-faint">
              Організатор проводить турніри. Роль надає адміністратор — опишіть, навіщо вона вам.
            </p>

            {lastDeclined && (
              <div className="notice notice-error">
                Попередню заявку відхилено
                {lastDeclined.responseNote ? `: ${lastDeclined.responseNote}` : "."}
              </div>
            )}

            <label className="field">
              Обґрунтування
              <textarea
                value={message}
                onChange={(event) => setMessage(event.target.value)}
                rows={3}
                maxLength={500}
                placeholder="Необовʼязково, але допомагає розглянути заявку швидше"
                className="input"
              />
            </label>

            <button
              type="button"
              onClick={() => run(() => organizerRequestsApi.apply(message))}
              className="btn btn-primary"
            >
              Подати заявку
            </button>
          </>
        )}

        {submitError.error && <div className="notice notice-error">{submitError.error}</div>}
      </div>
    </details>
  );
};

export default OrganizerRequestPanel;
