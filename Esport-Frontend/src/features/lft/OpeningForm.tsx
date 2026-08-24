import { useState } from "react";
import type { FormEvent } from "react";
import { lftApi } from "../../api/lftApi";
import { GAMES, gameLabel } from "../../constants/games";
import { positionsFor, positionLabel } from "../../constants/gamePositions";
import { ranksFor, rankLabel } from "../../constants/gameRanks";
import { UTC_OFFSETS, offsetLabel } from "../../constants/timeZones";
import { useSubmitError } from "../../hooks/useSubmitError";
import type { TeamOpeningDto } from "../../types";

type Props = {
  teamId: number;
  onSaved: (opening: TeamOpeningDto) => void;
  onCancel: () => void;
};

/**
 * Вакансія у складі. Дисципліна стоїть на вакансії, а не на команді, бо
 * команда може шукати гравців одразу у дві гри. Порожні поля означають
 * «будь-який» — і вакансія без вимог бачить також тих, хто рангу чи поясу
 * не вказав.
 */
export const OpeningForm = ({ teamId, onSaved, onCancel }: Props) => {
  const [game, setGame] = useState("");
  const [position, setPosition] = useState("");
  const [minRank, setMinRank] = useState("");
  const [minOffset, setMinOffset] = useState<number | null>(null);
  const [maxOffset, setMaxOffset] = useState<number | null>(null);
  const [pitch, setPitch] = useState("");
  const [busy, setBusy] = useState(false);
  const submitError = useSubmitError();

  // Зміна дисципліни знецінює роль і ранг: вони належали попередній грі,
  // і сервер відхилить ранг чи роль, яких у новій грі не існує.
  const changeGame = (value: string) => {
    setGame(value);
    setPosition("");
    setMinRank("");
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    submitError.clear();
    try {
      const saved = await lftApi.createOpening({
        teamId,
        game,
        position,
        minRank,
        minUtcOffsetMinutes: minOffset,
        maxUtcOffsetMinutes: maxOffset,
        pitch
      });
      onSaved(saved);
    } catch (error) {
      submitError.capture(error, "Не вдалося створити вакансію.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <form className="panel-body space-y-4" onSubmit={submit}>
      {submitError.error && <p className="notice notice-error">{submitError.error}</p>}

      <label className="field">
        Дисципліна
        <select
          className="input"
          value={game}
          onChange={(event) => changeGame(event.target.value)}
          required
        >
          <option value="">Оберіть дисципліну</option>
          {GAMES.map((value) => (
            <option key={value} value={value}>
              {gameLabel(value)}
            </option>
          ))}
        </select>
      </label>

      <label className="field">
        Роль
        <select
          className="input"
          value={position}
          onChange={(event) => setPosition(event.target.value)}
          disabled={!game}
        >
          <option value="">Будь-яка роль</option>
          {positionsFor(game).map((value) => (
            <option key={value} value={value}>
              {positionLabel(value)}
            </option>
          ))}
        </select>
      </label>

      <label className="field">
        Мінімальний ранг
        <select
          className="input"
          value={minRank}
          onChange={(event) => setMinRank(event.target.value)}
          disabled={!game}
        >
          <option value="">Будь-який ранг</option>
          {ranksFor(game).map((value) => (
            <option key={value} value={value}>
              {rankLabel(game, value)} і вище
            </option>
          ))}
        </select>
      </label>

      <div className="grid grid-cols-2 gap-3">
        <label className="field">
          Пояс: нижня межа
          <select
            className="input"
            value={minOffset ?? ""}
            onChange={(event) =>
              setMinOffset(event.target.value === "" ? null : Number(event.target.value))
            }
          >
            <option value="">Без нижньої межі</option>
            {UTC_OFFSETS.map((minutes) => (
              <option key={minutes} value={minutes}>
                {offsetLabel(minutes)}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          Пояс: верхня межа
          <select
            className="input"
            value={maxOffset ?? ""}
            onChange={(event) =>
              setMaxOffset(event.target.value === "" ? null : Number(event.target.value))
            }
          >
            <option value="">Без верхньої межі</option>
            {UTC_OFFSETS.map((minutes) => (
              <option key={minutes} value={minutes}>
                {offsetLabel(minutes)}
              </option>
            ))}
          </select>
        </label>
      </div>

      <label className="field">
        Опис
        <textarea
          className="input"
          maxLength={300}
          value={pitch}
          placeholder="Кого шукаєте і коли граєте"
          onChange={(event) => setPitch(event.target.value)}
        />
      </label>

      <div className="flex gap-2">
        <button type="submit" className="btn btn-primary" disabled={busy || !game}>
          Опублікувати
        </button>
        <button type="button" className="btn btn-ghost" onClick={onCancel} disabled={busy}>
          Скасувати
        </button>
      </div>
    </form>
  );
};

export default OpeningForm;
