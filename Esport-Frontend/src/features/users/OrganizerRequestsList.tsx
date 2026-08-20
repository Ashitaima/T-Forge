import { useCallback, useEffect, useState } from "react";
import { organizerRequestsApi } from "../../api/organizerRequestsApi";
import { EmptyState, PageHeader, Skeleton } from "../../components/ui/Primitives";
import { useSubmitError } from "../../hooks/useSubmitError";
import type { OrganizerRequestDto, OrganizerRequestStatus } from "../../types";

const STATUS_LABELS: Record<OrganizerRequestStatus, string> = {
  Pending: "Чекає розгляду",
  Approved: "Схвалено",
  Declined: "Відмовлено",
  Cancelled: "Відкликано"
};

const STATUS_PILL: Record<OrganizerRequestStatus, string> = {
  Pending: "pill-neutral",
  Approved: "pill-done",
  Declined: "pill-off",
  Cancelled: "pill-off"
};

const TABS: { value: OrganizerRequestStatus | ""; label: string }[] = [
  { value: "Pending", label: "Чекають" },
  { value: "", label: "Усі" }
];

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("uk-UA", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit"
  });

/**
 * Черга заявок на роль організатора.
 *
 * Роль дає право створювати турніри, тож видає її лише адміністратор —
 * реєстрація натомість лишає заявку (див. Common/OrganizerRequestPolicy.cs).
 */
const OrganizerRequestsList = () => {
  const [requests, setRequests] = useState<OrganizerRequestDto[]>([]);
  const [status, setStatus] = useState<OrganizerRequestStatus | "">("Pending");
  const [loading, setLoading] = useState(true);
  const [decliningId, setDecliningId] = useState<number | null>(null);
  const [note, setNote] = useState("");
  const submitError = useSubmitError();

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setRequests(await organizerRequestsApi.getAll(status || undefined));
    } catch {
      setRequests([]);
    } finally {
      setLoading(false);
    }
  }, [status]);

  useEffect(() => {
    load();
  }, [load]);

  const run = async (action: () => Promise<unknown>) => {
    submitError.clear();
    try {
      await action();
      await load();
    } catch (error) {
      submitError.capture(error);
    }
  };

  const decline = async (id: number) => {
    await run(() => organizerRequestsApi.decline(id, note));
    setDecliningId(null);
    setNote("");
  };

  return (
    <>
      <PageHeader
        eyebrow="Адміністрування"
        title="Заявки на роль організатора"
        description="Схвалення надає право створювати турніри."
      />

      <div className="flex gap-1.5">
        {TABS.map((tab) => (
          <button
            key={tab.label}
            type="button"
            onClick={() => setStatus(tab.value)}
            aria-pressed={status === tab.value}
            className={`rounded-lg px-3 py-1.5 text-micro transition ${
              status === tab.value
                ? "bg-ink-800 text-text"
                : "text-text-muted hover:bg-ink-900 hover:text-text"
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {submitError.error && <div className="notice notice-error">{submitError.error}</div>}

      <section className="panel">
        <div className="panel-body space-y-3">
          {loading && <Skeleton rows={3} />}

          {!loading && requests.length === 0 && (
            <EmptyState
              title="Заявок немає"
              hint="Тут зʼявляться запити на роль організатора — їх подають під час реєстрації або з профілю."
            />
          )}

          {!loading &&
            requests.map((request) => (
              <div key={request.id} className="surface-raised px-4 py-3.5">
                <div className="flex flex-wrap items-center gap-3">
                  <div className="min-w-0 flex-1">
                    <div className="text-body text-text">{request.username}</div>
                    <div className="muted text-micro">{request.email}</div>
                  </div>

                  <span className="tabular shrink-0 font-mono text-micro text-text-faint">
                    {formatDateTime(request.createdAt)}
                  </span>
                  <span className={`pill shrink-0 ${STATUS_PILL[request.status]}`}>
                    {STATUS_LABELS[request.status]}
                  </span>

                  {request.status === "Pending" && (
                    <div className="row-actions ml-auto">
                      <button
                        type="button"
                        onClick={() => run(() => organizerRequestsApi.approve(request.id))}
                        className="btn btn-primary btn-sm"
                      >
                        Схвалити
                      </button>
                      <button
                        type="button"
                        onClick={() =>
                          setDecliningId((current) => (current === request.id ? null : request.id))
                        }
                        className="btn btn-ghost btn-sm"
                      >
                        Відмовити
                      </button>
                    </div>
                  )}
                </div>

                {request.message && <p className="muted mt-2 text-micro">{request.message}</p>}

                {request.responseNote && (
                  <p className="mt-2 text-micro text-text-muted">
                    Причина відмови: {request.responseNote}
                  </p>
                )}

                {decliningId === request.id && (
                  <div className="mt-3 flex flex-wrap items-end gap-3 border-t border-line-soft pt-3">
                    <label className="field min-w-0 flex-1">
                      Причина
                      <input
                        type="text"
                        value={note}
                        onChange={(event) => setNote(event.target.value)}
                        placeholder="Необовʼязково — але заявник її побачить"
                        className="input"
                      />
                    </label>
                    <button
                      type="button"
                      onClick={() => decline(request.id)}
                      className="btn btn-danger btn-sm"
                    >
                      Підтвердити відмову
                    </button>
                  </div>
                )}
              </div>
            ))}
        </div>
      </section>
    </>
  );
};

export default OrganizerRequestsList;
