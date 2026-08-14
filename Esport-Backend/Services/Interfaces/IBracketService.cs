using TForge.Models;

namespace TForge.Services.Interfaces
{
    public interface IBracketService
    {
        /// <summary>Створює перший раунд сітки. Повертає кількість створених матчів.</summary>
        Task<int> GenerateAsync(int tournamentId);

        /// <summary>Створює наступний раунд, коли поточний повністю завершено.</summary>
        Task AdvanceAsync(Match completedMatch);
    }
}
