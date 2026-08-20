import { useState } from "react";
import { Plus, X } from "lucide-react";
import { playersApi } from "../../api/playersApi";
import { GAMES, gameLabel } from "../../constants/games";
import { positionLabel, positionsFor } from "../../constants/gamePositions";
import { useSubmitError } from "../../hooks/useSubmitError";
import type { PlayerGameProfileDto } from "../../types";

type Props = {
  playerId: number;
  profiles: PlayerGameProfileDto[];
  onChange: (profiles: PlayerGameProfileDto[]) => void;
};

/**
 * Дисципліни гравця й роль у кожній.
 *
 * Player.position лишається однією позицією на профіль; тут — рядок на кожну
 * гру, бо один гравець буває Duelist у Valorant і AWPer у CS2. Список позицій
 * залежить від обраної дисципліни (див. constants/gamePositions.ts): сервер
 * відхиляє позицію, що не належить грі.
 */
export const GameProfilesPanel = ({ playerId, profiles, onChange }: Props) => {
  const [game, setGame] = useState<string>("");
  const [position, setPosition] = useState<string>("");
  const [busy, setBusy] = useState(false);
  const submitError = useSubmitError();

  const positions = positionsFor(game);

  const save = async () => {
    if (!game) {
      return;
    }

    setBusy(true);
    submitError.clear();
    try {
      const saved = await playersApi.saveGameProfile(playerId, { game, position });
      // Та сама дисципліна оновлює роль, а не додає рядок — тож замінюємо
      // наявний запис, якщо він уже був.
      const rest = profiles.filter((item) => item.game !== saved.game);
      onChange([...rest, saved]);
      setGame("");
      setPosition("");
    } catch (error) {
      submitError.capture(error, "Не вдалося зберегти дисципліну.");
    } finally {
      setBusy(false);
    }
  };

  const remove = async (profileId: number) => {
    setBusy(true);
    submitError.clear();
    try {
      await playersApi.removeGameProfile(playerId, profileId);
      onChange(profiles.filter((item) => item.id !== profileId));
    } catch (error) {
      submitError.capture(error, "Не вдалося прибрати дисципліну.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="panel">
      <div className="panel-header">
        <h2 className="section-title">Дисципліни</h2>
      </div>
      <div className="panel-body space-y-4">
        <p className="text-micro text-text-faint">
          Ігри, у які ви граєте, і роль у кожній. Одну гру можна вказати лише раз.
        </p>

        {profiles.length > 0 && (
          <ul className="space-y-2">
            {profiles.map((profile) => (
              <li
                key={profile.id}
                className="surface-raised flex items-center gap-3 px-4 py-3"
              >
                <div className="min-w-0 flex-1">
                  <div className="text-body text-text">{gameLabel(profile.game)}</div>
                  <div className="muted text-micro">
                    {profile.position ? positionLabel(profile.position) : "Роль не вказано"}
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => remove(profile.id)}
                  disabled={busy}
                  className="btn btn-ghost btn-sm px-2"
                  aria-label={`Прибрати ${gameLabel(profile.game)}`}
                >
                  <X className="h-4 w-4" />
                </button>
              </li>
            ))}
          </ul>
        )}

        <div className="grid gap-3 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
          <label className="field">
            Дисципліна
            <select
              value={game}
              onChange={(event) => {
                setGame(event.target.value);
                // Позиції залежать від гри: збережена роль від попередньої
                // дисципліни тут не пройшла б валідацію.
                setPosition("");
              }}
              className="input"
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
            Роль <span className="text-micro font-normal text-text-faint">— необов&#39;язково</span>
            <select
              value={position}
              onChange={(event) => setPosition(event.target.value)}
              disabled={!game}
              className="input"
            >
              <option value="">Не вказано</option>
              {positions.map((value) => (
                <option key={value} value={value}>
                  {positionLabel(value)}
                </option>
              ))}
            </select>
          </label>

          <button
            type="button"
            onClick={save}
            disabled={!game || busy}
            className="btn btn-secondary"
          >
            <Plus className="h-4 w-4" />
            Додати
          </button>
        </div>

        {submitError.error && <div className="notice notice-error">{submitError.error}</div>}
      </div>
    </section>
  );
};

export default GameProfilesPanel;
