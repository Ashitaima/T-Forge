import { useEffect, useState } from "react";
import { playersApi } from "../api/playersApi";
import { useAuthStore } from "../store/authStore";

export type MyPlayer = { playerId: number; nickname: string };

/**
 * Профіль гравця поточного користувача — або null, якщо його немає.
 *
 * Нікнейм не входить до UserDto: він живе на профілі гравця, а профіль є не в
 * кожного — організатор і адміністратор без нього обходяться, і GET
 * /api/players/me для них відповідає 404. Тож null тут означає рівно «цей
 * користувач не гравець», і меню просто не показує пункт публічного профілю.
 */
export const useMyPlayer = () => {
  const user = useAuthStore((state) => state.user);
  const userId = user?.id ?? 0;
  const [player, setPlayer] = useState<MyPlayer | null>(null);

  useEffect(() => {
    if (userId === 0) {
      setPlayer(null);
      return;
    }

    let isActive = true;

    playersApi
      .getMe()
      .then((data) => isActive && setPlayer({ playerId: data.id, nickname: data.nickname }))
      .catch(() => isActive && setPlayer(null));

    return () => {
      isActive = false;
    };
  }, [userId]);

  return player;
};
