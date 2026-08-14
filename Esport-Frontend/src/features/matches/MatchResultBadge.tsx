/** Підсумок матчу однією позначкою: перемога, поразка або ще не зіграно. */
export const MatchResultBadge = ({ result }: { result: string }) => {
  if (result === "Win") {
    return <span className="pill pill-done">Перемога</span>;
  }

  if (result === "Loss") {
    return <span className="pill pill-off">Поразка</span>;
  }

  return <span className="pill pill-neutral">Не зіграно</span>;
};
