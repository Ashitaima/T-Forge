namespace TForge.Common
{
    /// <summary>
    /// Хто має право передати капітанство команди.
    ///
    /// Чиста функція без EF, як FriendlyMatchPolicy і TournamentOwnershipPolicy.
    /// Капітанство — не роль, а колонка Team.CaptainId, тож перевірити його
    /// атрибутом [Authorize] неможливо: право випливає з самого запису.
    ///
    /// Передати капітанство може чинний капітан — він же і втрачає його —
    /// або адміністратор, який лишається над правилом, як і всюди в застосунку.
    /// Це навмисно єдиний спосіб змінити CaptainId: інакше команда, чий капітан
    /// пішов, лишалася б без керівника назавжди.
    /// </summary>
    public static class TeamCaptaincyPolicy
    {
        /// <summary>Обидва id — це id користувачів: Team.CaptainId посилається на User.</summary>
        public static bool CanTransfer(int currentCaptainId, int userId, bool isAdmin) =>
            isAdmin || (currentCaptainId != 0 && currentCaptainId == userId);
    }
}
