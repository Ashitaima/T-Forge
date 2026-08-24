using System.Linq.Expressions;
using TForge.Models;

namespace TForge.Common
{
    /// <summary>
    /// Кого вакансія бачить у стрічці — чиста функція, як і решта правил
    /// у Common.
    ///
    /// Правило одне, а працює у два боки: гравець гортає вакансії, капітан
    /// гортає кандидатів. Написане двічі, воно неминуче розійшлося б — тож
    /// написане деревом виразів: EF перекладає його в SQL для обох стрічок,
    /// а Compile() дає той самий предикат для перевірки при змаху. Той самий
    /// підхід, що в PlayerSortKeys, де ключ віддає вираз сортування.
    ///
    /// Усі умови — жорсткий фільтр: картка або підходить, або її немає.
    /// Ваги й «сумісність у відсотках» свідомо не робимо — їх не порахувати
    /// в SQL, а підбирати їх немає на чому.
    /// </summary>
    public static class LftEligibility
    {
        /// <summary>
        /// Лише те, що потрібно правилу. Приймати сам TeamOpening було б
        /// коротше, але тоді правило залежало б від EF-сутності й перестало
        /// бути чистим.
        /// </summary>
        public record Requirements(
            string Game,
            string Position,
            string MinRank,
            int? MinUtcOffsetMinutes,
            int? MaxUtcOffsetMinutes);

        public static Requirements From(TeamOpening opening) =>
            new(opening.Game,
                opening.Position,
                opening.MinRank,
                opening.MinUtcOffsetMinutes,
                opening.MaxUtcOffsetMinutes);

        /// <summary>
        /// Предикат кандидата. Саме Expression, а не Func: цей екземпляр іде
        /// і у Where() запиту, і — через Compile() — у перевірку при змаху.
        /// </summary>
        public static Expression<Func<Player, bool>> Predicate(Requirements requirements)
        {
            // Межу рангу перекладаємо в перелік значень заздалегідь: у SQL це
            // стане IN (...), бо покликати там GameRanks нема як.
            var acceptableRanks = GameRanks.AtOrAbove(requirements.Game, requirements.MinRank);

            var anyPosition = string.IsNullOrEmpty(requirements.Position);
            var position = requirements.Position;
            var game = requirements.Game;
            var min = requirements.MinUtcOffsetMinutes;
            var max = requirements.MaxUtcOffsetMinutes;

            return player =>
                player.IsActive
                && player.TeamId == null
                && player.GameProfiles.Any(profile =>
                    profile.Game == game
                    && profile.IsLookingForTeam
                    && (anyPosition || profile.Position == position)
                    && acceptableRanks.Contains(profile.Rank))
                // Гравець без поясу проходить лише у вікно без меж — так само,
                // як гравець без рангу лише у вакансію без вимоги.
                && ((min == null && max == null)
                    || (player.UtcOffsetMinutes != null
                        && (min == null || player.UtcOffsetMinutes >= min)
                        && (max == null || player.UtcOffsetMinutes <= max)));
        }
    }
}
