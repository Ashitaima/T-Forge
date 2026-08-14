import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import type { HubConnection } from "@microsoft/signalr";

const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5274";

export type ScoreUpdate = {
  matchId: number;
  homeTeamScore: number;
  awayTeamScore: number;
};

export type StatusUpdate = {
  matchId: number;
  status: string;
  homeTeamScore: number;
  awayTeamScore: number;
  winnerTeamId?: number | null;
};

/**
 * Одне зʼєднання на всю вкладку: підписки на окремі матчі — це групи на сервері,
 * тож відкриття кількох сторінок не створює зайвих сокетів.
 */
let connection: HubConnection | null = null;
let starting: Promise<HubConnection> | null = null;

const getConnection = () => {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/matches`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
  }
  return connection;
};

const ensureStarted = async () => {
  const hub = getConnection();

  if (hub.state === HubConnectionState.Connected) {
    return hub;
  }

  if (!starting) {
    starting = hub
      .start()
      .then(() => hub)
      .finally(() => {
        starting = null;
      });
  }

  return starting;
};

/**
 * Підписує на живі оновлення матчу. Повертає функцію відписки —
 * викликайте її при розмонтуванні компонента.
 */
export const subscribeToMatch = (
  matchId: number,
  handlers: { onScore?: (update: ScoreUpdate) => void; onStatus?: (update: StatusUpdate) => void }
) => {
  let cancelled = false;

  const score = (update: ScoreUpdate) => {
    if (update.matchId === matchId) {
      handlers.onScore?.(update);
    }
  };

  const status = (update: StatusUpdate) => {
    if (update.matchId === matchId) {
      handlers.onStatus?.(update);
    }
  };

  ensureStarted()
    .then(async (hub) => {
      if (cancelled) {
        return;
      }
      hub.on("ScoreUpdated", score);
      hub.on("MatchStatusChanged", status);
      await hub.invoke("SubscribeToMatch", matchId);
    })
    // Живі оновлення — покращення, а не умова роботи сторінки:
    // якщо хаб недоступний, дані все одно завантажаться через REST.
    .catch(() => undefined);

  return () => {
    cancelled = true;
    const hub = getConnection();
    hub.off("ScoreUpdated", score);
    hub.off("MatchStatusChanged", status);

    if (hub.state === HubConnectionState.Connected) {
      hub.invoke("UnsubscribeFromMatch", matchId).catch(() => undefined);
    }
  };
};
