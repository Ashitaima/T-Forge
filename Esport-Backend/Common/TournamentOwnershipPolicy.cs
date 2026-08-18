namespace TForge.Common
{
    /// <summary>
    /// Хто має право змінювати турнір: редагувати його та генерувати сітку.
    ///
    /// Чиста функція без EF і сервісів — як FriendlyMatchPolicy та
    /// MembershipRequestPolicy. Роль «організатор» дає право створювати власні
    /// турніри, але не чіпати чужі: інакше будь-який організатор міг би змінити
    /// дисципліну, дати чи статус турніру, до якого не має стосунку.
    ///
    /// Адміністратор лишається над цим правилом, як і всюди в застосунку.
    /// </summary>
    public static class TournamentOwnershipPolicy
    {
        /// <summary>Обидва id — це id користувачів: Tournament.OrganizerId посилається на User.</summary>
        public static bool CanManage(int organizerId, int userId, bool isAdmin) =>
            isAdmin || (organizerId != 0 && organizerId == userId);
    }
}
