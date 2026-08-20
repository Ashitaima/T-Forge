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
        /// <summary>
        /// Чи може користувач діяти від імені команди — передати капітанство,
        /// змінити логотип тощо. Обидва id — це id користувачів:
        /// Team.CaptainId посилається на User.
        /// </summary>
        public static bool CanManage(int currentCaptainId, int userId, bool isAdmin) =>
            isAdmin || (currentCaptainId != 0 && currentCaptainId == userId);

        /// <summary>
        /// Передати капітанство може той самий, хто взагалі керує командою.
        /// Окреме ім'я лишається, бо на місці виклику воно читається точніше;
        /// правило ж має бути одне, інакше дві копії з часом розійдуться.
        /// </summary>
        public static bool CanTransfer(int currentCaptainId, int userId, bool isAdmin) =>
            CanManage(currentCaptainId, userId, isAdmin);
    }
}
