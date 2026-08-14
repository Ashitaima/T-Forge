import { Fragment } from "react";
import { Trophy } from "lucide-react";
import type { MatchDto } from "../../types";

const roundNames: Record<string, string> = {
  PlayIn: "Кваліфікація",
  RoundOf32: "1/16 фіналу",
  RoundOf16: "1/8 фіналу",
  QuarterFinal: "Чвертьфінал",
  SemiFinal: "Півфінал",
  Final: "Фінал",
  ThirdPlace: "За третє місце",
  GroupStage: "Груповий етап"
};

/**
 * Місце команди у вузлі сітки. Переможець підсвічений, той хто вибув — приглушений,
 * тож шлях команди до фіналу читається по вертикалі без легенди.
 */
const Seat = ({
  name,
  score,
  outcome
}: {
  name: string;
  score: number;
  outcome: "won" | "lost" | "pending";
}) => (
  <div
    className={`bracket-seat ${
      outcome === "won" ? "bracket-seat-won" : outcome === "lost" ? "bracket-seat-lost" : ""
    }`}
  >
    <span className="flex min-w-0 items-center gap-2">
      {outcome === "won" && <span className="h-3 w-0.5 shrink-0 rounded-full bg-win" />}
      <span className="truncate">{name}</span>
    </span>
    <span className="tabular shrink-0 font-mono text-micro">{score}</span>
  </div>
);

/** Невидимий двійник підпису раунду — тримає колонки звʼязків на одній висоті. */
const LabelSpacer = () => (
  <div className="invisible mb-1 flex items-baseline gap-2" aria-hidden>
    <span className="eyebrow">&nbsp;</span>
  </div>
);

/**
 * Колонка звʼязків між раундами.
 *
 * Справжнє дерево малюємо лише тоді, коли наступний раунд рівно вдвічі менший —
 * тоді кожна пара зводиться в один матч. Якщо ж у раунд входять команди, що
 * пройшли без гри (bye), така пара не відповідає дійсності, і ми показуємо
 * просту лінію передачі замість вигаданої структури.
 */
const Connector = ({ pairs, merge }: { pairs: number; merge: boolean }) => (
  <div className="flex w-8 shrink-0 flex-col" aria-hidden>
    <LabelSpacer />
    <div className="flex flex-1 flex-col">
      {Array.from({ length: pairs }).map((_, index) => (
        <div key={index} className="flex-1">
          <svg className="h-full w-full" preserveAspectRatio="none" viewBox="0 0 32 100">
            <path
              d={merge ? "M0 25 H16 V75 H0 M16 50 H32" : "M0 50 H32"}
              fill="none"
              stroke="#2A3340"
              strokeWidth="1.5"
              vectorEffect="non-scaling-stroke"
            />
          </svg>
        </div>
      ))}
    </div>
  </div>
);

export const BracketView = ({ matches }: { matches: MatchDto[] }) => {
  const bracketMatches = matches.filter((match) => match.round > 0);

  if (bracketMatches.length === 0) {
    return null;
  }

  const grouped = new Map<number, MatchDto[]>();
  bracketMatches.forEach((match) => {
    grouped.set(match.round, [...(grouped.get(match.round) ?? []), match]);
  });
  const rounds = [...grouped.entries()]
    .sort((a, b) => a[0] - b[0])
    .map(([round, list]) => [round, [...list].sort((a, b) => a.id - b.id)] as const);

  const finalRound = rounds[rounds.length - 1]?.[1] ?? [];
  const champion =
    finalRound.length === 1 && finalRound[0].status === "Completed" ? finalRound[0].winnerTeam : null;

  return (
    <div className="bracket-rail">
      {rounds.map(([round, roundMatches], roundIndex) => {
        const previous = roundIndex > 0 ? rounds[roundIndex - 1][1] : null;
        const isMerge = previous ? roundMatches.length === previous.length / 2 : false;

        return (
          <Fragment key={round}>
            {previous && (
              <Connector pairs={isMerge ? roundMatches.length : previous.length} merge={isMerge} />
            )}

            <div className="bracket-round">
              <div className="mb-1 flex items-baseline gap-2">
                <span className="eyebrow">
                  {roundNames[roundMatches[0].matchType] ?? roundMatches[0].matchType}
                </span>
                <span className="font-mono text-micro text-text-faint">R{round}</span>
              </div>

              {/* Матчі розподілені рівномірно — саме на ці позиції спираються звʼязки */}
              <div className="flex flex-1 flex-col justify-around gap-4">
                {roundMatches.map((match) => {
                  const decided = match.status === "Completed" && match.winnerTeam;
                  const homeOutcome = !decided
                    ? "pending"
                    : match.winnerTeam?.id === match.homeTeam?.id
                      ? "won"
                      : "lost";
                  const awayOutcome = !decided
                    ? "pending"
                    : match.winnerTeam?.id === match.awayTeam?.id
                      ? "won"
                      : "lost";

                  return (
                    <article
                      key={match.id}
                      className={`bracket-node ${match.status === "InProgress" ? "bracket-node-live" : ""}`}
                    >
                      <Seat
                        name={match.homeTeam?.name ?? "Очікується"}
                        score={match.homeTeamScore}
                        outcome={homeOutcome}
                      />
                      <div className="h-px bg-line-soft" />
                      <Seat
                        name={match.awayTeam?.name ?? "Очікується"}
                        score={match.awayTeamScore}
                        outcome={awayOutcome}
                      />
                      <div className="flex items-center justify-between border-t border-line-soft bg-ink-950/40 px-3 py-1.5">
                        <span className="font-mono text-micro text-text-faint">{match.format}</span>
                        <span
                          className={`font-mono text-micro ${
                            match.status === "InProgress" ? "text-ember" : "text-text-faint"
                          }`}
                        >
                          {match.status === "InProgress"
                            ? "у грі"
                            : new Date(match.scheduledAt).toLocaleDateString("uk-UA", {
                                day: "2-digit",
                                month: "short"
                              })}
                        </span>
                      </div>
                    </article>
                  );
                })}
              </div>
            </div>
          </Fragment>
        );
      })}

      {champion && (
        <>
          <Connector pairs={1} merge={false} />
          {/* Клеймо чемпіона — єдине місце, де дисплейний шрифт зʼявляється у сітці */}
          <div className="flex min-w-[13rem] flex-col">
            <div className="mb-1 flex items-baseline">
              <span className="eyebrow">Чемпіон</span>
            </div>
            <div className="flex flex-1 flex-col justify-around">
              <div className="rounded-lg border border-ember/40 bg-ember/[0.07] px-4 py-4 shadow-heat">
                <Trophy className="h-5 w-5 text-ember" />
                <div className="mt-2.5 font-display text-lead font-extrabold leading-tight text-text">
                  {champion.name}
                </div>
                <div className="mt-1 font-mono text-micro text-ember-soft">{champion.tag}</div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
};
