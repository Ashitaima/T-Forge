namespace TForge.Common
{
    /// <summary>
    /// Права навколо пошуку складу — чисті функції, як FriendlyMatchPolicy
    /// і MatchChallengePolicy. Усі id — це id користувачів: Team.CaptainId
    /// посилається на User.
    /// </summary>
    public static class LftPolicy
    {
        /// <summary>
        /// Публікувати, правити й закривати вакансію може капітан команди —
        /// або адміністратор, який лишається над правилом власності.
        /// Право випливає з колонки Team.CaptainId, а не з ролі, тож
        /// атрибутом [Authorize] його не перевірити.
        /// </summary>
        public static bool CanManageOpening(int teamCaptainUserId, int userId, bool isAdmin) =>
            TeamCaptaincyPolicy.CanManage(teamCaptainUserId, userId, isAdmin);

        /// <summary>
        /// Чи може користувач змахнути як гравець по цій вакансії.
        ///
        /// Параметра isAdmin тут навмисно немає — свідомий виняток із правила
        /// «адміністратор вище за все», такий самий, як DraftPolicy.CanAct.
        /// Змах — це згода сторони, а згода, записана за когось іншого, лишає
        /// запис, який бреше про те, що сталося. Капітан не змахує по власній
        /// вакансії з тієї ж причини, з якої MatchChallengePolicy.CanRespond
        /// відмовляє ініціаторові: інакше в складі опинився б гравець, який
        /// нікому згоди не давав.
        /// </summary>
        public static bool CanSwipeAsPlayer(int openingTeamCaptainUserId, int playerUserId) =>
            openingTeamCaptainUserId != playerUserId;
    }
}
