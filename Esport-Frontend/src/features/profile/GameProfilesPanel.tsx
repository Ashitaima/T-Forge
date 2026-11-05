import { useState } from "react";
import { Plus, X } from "lucide-react";
import { playersApi } from "../../api/playersApi";
import { GAMES, gameLabel } from "../../constants/games";
import { positionLabel, positionsFor } from "../../constants/gamePositions";
import { rankLabel, ranksFor } from "../../constants/gameRanks";
import { CHESS_RATING_MAX, CHESS_RATING_MIN, chessBandFor } from "../../constants/chessRatings";
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
  const [rank, setRank] = useState<string>("");
  // Лише для шахів: точне число, з якого сервер сам виведе смугу.
  const [rating, setRating] = useState<string>("");
  const [lookingForTeam, setLookingForTeam] = useState(false);
  const [busy, setBusy] = useState(false);
  const submitError = useSubmitError();

  const positions = positionsFor(game);
  const ranks = ranksFor(game);
  const isChess = game === "Chess";
  const exactRating = isChess && rating.trim() !== "" ? Number(rating) : null;
  // Коли рейтинг введено, смуга — його наслідок, а не окремий вибір.
  const shownRank =
    exactRating !== null && Number.isFinite(exactRating) ? chessBandFor(exactRating) : rank;

  const save = async () => {
    if (!game) {
      return;
    }

    setBusy(true);
    submitError.clear();
    try {
      const saved = await playersApi.saveGameProfile(playerId, {
        game,
        position,
        rank: shownRank,
        rating: exactRating,
        isLookingForTeam: lookingForTeam
      });
      // Та сама дисципліна оновлює роль, а не додає рядок — тож замінюємо
      // наявний запис, якщо він уже був.
      const rest = profiles.filter((item) => item.game !== saved.game);
      onChange([...rest, saved]);
      setGame("");
      setPosition("");
      setRank("");
      setRating("");
      setLookingForTeam(false);
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
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-body text-text">{gameLabel(profile.game)}</span>
                    {profile.isLookingForTeam && <span className="pill">Шукає команду</span>}
                  </div>
                  <div className="muted text-micro">
                    {profile.position ? positionLabel(profile.position) : "Роль не вказано"}
                    {" · "}
                    {rankLabel(profile.game, profile.rank)}
                    {profile.rating !== null && (
                      <span className="font-mono tabular"> · {profile.rating}</span>
                    )}
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

        <div className="grid gap-3 sm:grid-cols-[repeat(auto-fit,minmax(9rem,1fr))] sm:items-end">
          <label className="field">
            Дисципліна
            <select
              value={game}
              onChange={(event) => {
                setGame(event.target.value);
                // Позиції й ранг залежать від гри: збережені значення від
                // попередньої дисципліни тут не пройшли б валідацію.
                setPosition("");
                setRank("");
                setRating("");
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

          <label className="field">
            Ранг <span className="text-micro font-normal text-text-faint">— необов&#39;язково</span>
            <select
              className="input"
              value={shownRank}
              onChange={(event) => setRank(event.target.value)}
              disabled={!game || busy || exactRating !== null}
            >
              <option value="">Ранг не вказано</option>
              {ranks.map((value) => (
                <option key={value} value={value}>{rankLabel(game, value)}</option>
              ))}
            </select>
          </label>

          {isChess && (
            <label className="field">
              Точний рейтинг{" "}
              <span className="text-micro font-normal text-text-faint">— необовʼязково</span>
              <input
                type="number"
                min={CHESS_RATING_MIN}
                max={CHESS_RATING_MAX}
                value={rating}
                onChange={(event) => setRating(event.target.value)}
                disabled={busy}
                placeholder="Lichess, chess.com або FIDE"
                className="input tabular font-mono"
              />
            </label>
          )}

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

        <label className="flex items-center gap-2 text-micro">
          <input
            type="checkbox"
            checked={lookingForTeam}
            onChange={(event) => setLookingForTeam(event.target.checked)}
            disabled={busy}
          />
          Шукаю команду в цій дисципліні
        </label>

        {submitError.error && <div className="notice notice-error">{submitError.error}</div>}
      </div>
    </section>
  );
};

export default GameProfilesPanel;
