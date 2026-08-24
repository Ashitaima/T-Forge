import { useCallback, useEffect, useMemo, useState } from "react";
import { lftApi } from "../../api/lftApi";
import { playersApi } from "../../api/playersApi";
import { teamInitials } from "../../components/ui/Avatar";
import { EmptyState, Skeleton } from "../../components/ui/Primitives";
import { gameLabel } from "../../constants/games";
import { positionLabel } from "../../constants/gamePositions";
import { rankLabel } from "../../constants/gameRanks";
import { offsetDistanceLabel, offsetLabel } from "../../constants/timeZones";
import { isOpeningOpen } from "../../constants/teamOpeningStatuses";
import { useIsCaptainOf } from "../../hooks/useEffectiveRole";
import { usePagedList } from "../../hooks/usePagedList";
import { useSubmitError } from "../../hooks/useSubmitError";
import { useAuthStore } from "../../store/authStore";
import { SwipeCard } from "./SwipeCard";
import type { PagedResponse, PlayerDto, PlayerSummaryDto, TeamOpeningDto } from "../../types";

/** Порожня сторінка — заглушка для боку, який зараз не читає стрічку. */
const emptyPage = <T,>(): PagedResponse<T> => ({
  data: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false
});

/**
 * Пошук складу. Гравець гортає вакансії, капітан — кандидатів.
 *
 * Бік визначає не роль, а наявність власної відкритої вакансії: капітанство
 * це колонка Team.CaptainId, і роллю його не виразити. UserDto не несе ні
 * teamId, ні часового поясу — обидва живуть на PlayerDto, тож команду й пояс
 * читаємо через playersApi.getMe().
 *
 * Порожня стрічка тут — звичайна річ (фільтри жорсткі), тож вона мусить
 * пояснювати, яка саме вимога нікого не лишила, інакше читається як помилка.
 */
export const LftPage = () => {
  const user = useAuthStore((state) => state.user);
  const [player, setPlayer] = useState<PlayerDto | null>(null);
  const [playerLoading, setPlayerLoading] = useState(true);
  const [myOpenings, setMyOpenings] = useState<TeamOpeningDto[]>([]);
  const [myOpeningsLoading, setMyOpeningsLoading] = useState(true);
  const [openingId, setOpeningId] = useState<number | null>(null);
  const [matchNotice, setMatchNotice] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const submitError = useSubmitError();

  // Профіль гравця дає команду (для капітанської сторони) і часовий пояс
  // (для сторони гравця) — жодного з двох UserDto не несе.
  useEffect(() => {
    if (!user) {
      setPlayer(null);
      setPlayerLoading(false);
      return;
    }
    let isActive = true;
    setPlayerLoading(true);
    playersApi
      .getMe()
      .then((data) => isActive && setPlayer(data))
      .catch(() => isActive && setPlayer(null))
      .finally(() => isActive && setPlayerLoading(false));
    return () => {
      isActive = false;
    };
  }, [user]);

  const team = player?.team ?? null;
  const isCaptain = useIsCaptainOf(team?.id, team?.captain?.id);

  // Вакансії, якими користувач керує сам: якщо серед них є відкрита — він
  // капітан у цій стрічці, і показуємо кандидатів, а не вакансії.
  useEffect(() => {
    if (!isCaptain || !team) {
      setMyOpenings([]);
      setMyOpeningsLoading(false);
      return;
    }
    let isActive = true;
    setMyOpeningsLoading(true);
    lftApi
      .openingsForTeam(team.id)
      .then((rows) => isActive && setMyOpenings(rows.filter((row) => isOpeningOpen(row.status))))
      .catch(() => isActive && setMyOpenings([]))
      .finally(() => isActive && setMyOpeningsLoading(false));
    return () => {
      isActive = false;
    };
  }, [isCaptain, team?.id]);

  const asCaptain = isCaptain && myOpenings.length > 0;
  const selected = useMemo(
    () => myOpenings.find((row) => row.id === openingId) ?? myOpenings[0] ?? null,
    [myOpenings, openingId]
  );

  // Кожна стрічка перезапитується, коли змінюється все, що читає її замикання:
  // для кандидатів це обрана вакансія, для вакансій — сам гравець і бік показу.
  // Поки триває інший бік, свій запит навмисно повертає порожню сторінку —
  // інакше обидва ендпойнти опитувалися б одночасно щоразу.
  const candidatesFetch = useCallback(
    (page: number, pageSize: number) =>
      selected
        ? lftApi.candidateFeed(selected.id, page, pageSize)
        : Promise.resolve(emptyPage<PlayerSummaryDto>()),
    [selected]
  );
  const {
    items: candidates,
    loading: candidatesLoading,
    reload: reloadCandidates
  } = usePagedList<PlayerSummaryDto>(candidatesFetch, `candidates:${selected?.id ?? "none"}`, 20);

  const openingsFetch = useCallback(
    (page: number, pageSize: number) =>
      asCaptain ? Promise.resolve(emptyPage<TeamOpeningDto>()) : lftApi.openingFeed(page, pageSize),
    [asCaptain]
  );
  const {
    items: openings,
    loading: openingsLoading,
    reload: reloadOpenings
  } = usePagedList<TeamOpeningDto>(openingsFetch, `openings:${asCaptain}:${player?.id ?? "none"}`, 20);

  const swipe = async (targetOpeningId: number, like: boolean, playerId?: number) => {
    setBusy(true);
    submitError.clear();
    try {
      const result = await lftApi.swipe(targetOpeningId, like, playerId);
      if (result.matched) {
        setMatchNotice(
          asCaptain
            ? "Взаємно — гравець приєднався до складу."
            : "Взаємно — ви приєдналися до складу команди."
        );
      }
      // Свайп, підтверджений сервером, більше не повертається у стрічку —
      // перечитуємо поточну сторінку замість локального фільтрування, щоб
      // не розходитись із тим, що справді бачить бекенд.
      if (asCaptain) {
        reloadCandidates();
      } else {
        reloadOpenings();
      }
    } catch (error) {
      submitError.capture(error, "Не вдалося надіслати відповідь.");
    } finally {
      setBusy(false);
    }
  };

  /** Те, чого вимагає вакансія, — рядком, для порожньої стрічки й для картки. */
  const requirements = (opening: TeamOpeningDto) =>
    [
      gameLabel(opening.game),
      opening.position ? positionLabel(opening.position) : "будь-яка роль",
      opening.minRank ? `${rankLabel(opening.game, opening.minRank)} і вище` : "будь-який ранг",
      opening.minUtcOffsetMinutes !== null || opening.maxUtcOffsetMinutes !== null
        ? `${offsetLabel(opening.minUtcOffsetMinutes)}…${offsetLabel(opening.maxUtcOffsetMinutes)}`
        : "будь-який пояс"
    ].join(" · ");

  const loading =
    playerLoading || myOpeningsLoading || (asCaptain ? candidatesLoading : openingsLoading);

  return (
    <div className="space-y-4">
      <h1 className="section-title">Пошук складу</h1>

      {matchNotice && <p className="notice notice-success">{matchNotice}</p>}
      {submitError.error && <p className="notice notice-error">{submitError.error}</p>}

      {asCaptain && myOpenings.length > 1 && (
        <select
          className="input max-w-md"
          value={selected?.id ?? ""}
          onChange={(event) => setOpeningId(Number(event.target.value))}
        >
          {myOpenings.map((opening) => (
            <option key={opening.id} value={opening.id}>{requirements(opening)}</option>
          ))}
        </select>
      )}

      {loading && <Skeleton rows={1} />}

      {!loading && asCaptain && selected && candidates.length === 0 && (
        <EmptyState
          title="Немає вільних гравців під ці вимоги"
          hint={`${requirements(selected)}. Спробуйте знизити межу рангу або розширити вікно часових поясів на сторінці команди.`}
        />
      )}

      {!loading &&
        asCaptain &&
        selected &&
        candidates.slice(0, 1).map((candidate) => (
          <SwipeCard
            key={candidate.id}
            title={candidate.nickname}
            subtitle={gameLabel(selected.game)}
            imageFallback={candidate.nickname.slice(0, 2)}
            facts={[
              { label: "Роль", value: positionLabel(candidate.position) || "—" },
              { label: "Країна", value: candidate.country || "—" }
            ]}
            busy={busy}
            onLike={() => swipe(selected.id, true, candidate.id)}
            onPass={() => swipe(selected.id, false, candidate.id)}
          />
        ))}

      {!loading && !asCaptain && openings.length === 0 && (
        <EmptyState
          title="Поки що немає вакансій під ваш профіль"
          hint="Перевірте, що у профілі відмічено «Шукаю команду», вказано ранг і часовий пояс — без них вакансії з вимогами вас не бачать."
        />
      )}

      {!loading &&
        !asCaptain &&
        openings.slice(0, 1).map((opening) => (
          <SwipeCard
            key={opening.id}
            title={opening.team?.name ?? "Команда"}
            subtitle={requirements(opening)}
            imageUrl={opening.team?.logoPath ?? null}
            imageFallback={teamInitials(opening.team?.name, opening.team?.tag)}
            facts={[
              { label: "Дисципліна", value: gameLabel(opening.game) },
              {
                label: "Пояс",
                value:
                  offsetDistanceLabel(opening.minUtcOffsetMinutes, player?.utcOffsetMinutes ?? null) ??
                  offsetLabel(opening.minUtcOffsetMinutes)
              }
            ]}
            body={opening.pitch}
            busy={busy}
            onLike={() => swipe(opening.id, true)}
            onPass={() => swipe(opening.id, false)}
          />
        ))}
    </div>
  );
};

export default LftPage;
