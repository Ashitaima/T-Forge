import { useRef, useState } from "react";
import { mlApi } from "../../api/mlApi";
import { useSubmitError } from "../../hooks/useSubmitError";

type Props = { disabled: boolean; onUploaded: () => void };

/**
 * Завантаження датасету.
 *
 * Схему файлу знає лише Python (ml/chess/datasets.py), тож усі змістовні
 * помилки формату приходять із сервера — без notice-error форма б просто
 * мовчала. Саме тому useSubmitError тут обов'язковий, а не «про всяк випадок».
 */
export const DatasetUploadForm = ({ disabled, onUploaded }: Props) => {
  const [name, setName] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const fileInput = useRef<HTMLInputElement>(null);
  const submitError = useSubmitError();

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    submitError.clear();

    if (!file) {
      return;
    }

    setBusy(true);
    try {
      await mlApi.uploadDataset(name, file);
      setName("");
      setFile(null);
      if (fileInput.current) {
        fileInput.current.value = "";
      }
      onUploaded();
    } catch (error) {
      submitError.capture(error, "Не вдалося завантажити датасет.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <form className="panel-body space-y-3" onSubmit={submit}>
      {submitError.error && <div className="notice notice-error">{submitError.error}</div>}

      <div className="grid gap-3 sm:grid-cols-2">
        <label className="block">
          <span className="eyebrow mb-1 block">Назва</span>
          <input
            className="input w-full"
            value={name}
            maxLength={100}
            placeholder="Наприклад, Lichess 2026-01"
            onChange={(event) => setName(event.target.value)}
          />
        </label>

        <label className="block">
          <span className="eyebrow mb-1 block">Файл CSV</span>
          <input
            ref={fileInput}
            className="input w-full"
            type="file"
            accept=".csv"
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
          />
        </label>
      </div>

      <p className="muted text-micro">
        Потрібні колонки: <span className="tabular">white_rating</span>,{" "}
        <span className="tabular">black_rating</span>, <span className="tabular">winner</span> (white /
        draw / black) і <span className="tabular">time_control</span> або{" "}
        <span className="tabular">increment_code</span>. Необов'язкова{" "}
        <span className="tabular">played_at</span> дає хронологічний поділ на навчання й тест. Формат
        Lichess розпізнається автоматично. Максимум 50 МБ, щонайменше 200 придатних партій.
      </p>

      <button className="btn btn-primary" type="submit" disabled={disabled || busy || !file || !name}>
        {busy ? "Перевіряю…" : "Завантажити"}
      </button>
    </form>
  );
};
