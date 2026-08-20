namespace TForge.Services.Interfaces
{
    /// <summary>
    /// Логотип команди. Право на зміну вирішує TeamCaptaincyPolicy — капітанство
    /// це колонка, а не роль, тож [Authorize(Roles = ...)] тут нічого не перевіряє.
    /// </summary>
    public interface ITeamLogoService
    {
        Task<string> SaveAsync(int teamId, IFormFile file, int requestingUserId, bool isAdmin);

        Task ClearAsync(int teamId, int requestingUserId, bool isAdmin);
    }
}
