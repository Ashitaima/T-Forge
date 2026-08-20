import { useEffect, useState } from "react";
import { teamsApi } from "../api/teamsApi";
import { useAuthStore } from "../store/authStore";
import type { TeamRowDto } from "../types";

/**
 * Команди, у яких поточний користувач — капітан.
 *
 * Капітанство — це `Team.CaptainId`, а не роль, тож `useEffectiveRole` про
 * нього нічого не знає й відповісти на питання «чи можу я створити матч»
 * не може. Порівнюємо саме з реальним `user.id`, а не з підставленою в
 * режимі розробника роллю: попередній перегляд чужої ролі не повинен
 * давати прав над чужою командою.
 *
 * Окремого ендпоінта під це немає, тож беремо сторінку команд і фільтруємо
 * на місці — список команд у цьому проєкті короткий.
 */
export const useCaptainedTeams = () => {
  const user = useAuthStore((state) => state.user);
  const [teams, setTeams] = useState<TeamRowDto[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!user) {
      setTeams([]);
      return;
    }

    let isActive = true;
    setLoading(true);

    teamsApi
      .getPaged({ page: 1, pageSize: 100, sortBy: "name", sortDirection: "asc" })
      .then((response) => {
        if (isActive) {
          setTeams(response.data.filter((team) => team.captainId === user.id));
        }
      })
      .catch(() => isActive && setTeams([]))
      .finally(() => isActive && setLoading(false));

    return () => {
      isActive = false;
    };
  }, [user]);

  return { teams, isCaptain: teams.length > 0, loading };
};
