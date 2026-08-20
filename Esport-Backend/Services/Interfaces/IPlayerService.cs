using TForge.DTOs;
using TForge.Common;
using TForge.Common.Filters;

namespace TForge.Services.Interfaces
{
    public interface IPlayerService
    {
        Task<IEnumerable<PlayerDto>> GetAllAsync();
        Task<PagedResponse<PlayerDto>> GetPagedAsync(PagedRequest request, PlayerFilter? filter = null);
        Task<PagedResponse<PlayerRowDto>> GetPagedRowsAsync(
            PagedRequest request,
            PlayerFilter? filter = null,
            int? viewerUserId = null,
            bool isAdmin = false);
        Task<PlayerDto?> GetByUserIdAsync(int userId);

        // Приховані поля профілю (імʼя, вік, країна) віддаються порожніми, поки
        // глядач не власник і не адміністратор — див. Common/ProfileVisibility.cs.
        // Типове значення означає «анонімний глядач», тобто найсуворіший режим:
        // забути передати глядача не має розкривати зайвого.
        Task<PlayerDto?> GetByIdAsync(int id, int? viewerUserId = null, bool isAdmin = false);
        Task<PlayerProfileDto> GetProfileAsync(int id, int? viewerUserId = null, bool isAdmin = false);
        Task<PlayerDto?> GetWithTeamAsync(int id);
        Task<PagedResponse<PlayerMatchDto>> GetMatchLogAsync(int id, PagedRequest request);
        Task<IEnumerable<PlayerDto>> GetByTeamAsync(int teamId);
        Task<IEnumerable<PlayerDto>> GetFreeAgentsAsync();
        Task<PlayerDto> CreateAsync(CreatePlayerDto createDto, int userId);
        Task<PlayerDto> CreateFullAsync(CreateFullPlayerDto createDto);
        Task<PlayerDto?> UpdateAsync(int id, UpdatePlayerDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> LeaveTeamAsync(int playerId);

        /// <summary>
        /// Додає дисципліну гравцеві або оновлює роль у вже доданій — пара
        /// (гравець, дисципліна) унікальна, тож повторне збереження тієї самої
        /// гри означає зміну ролі, а не другий рядок.
        /// </summary>
        Task<PlayerGameProfileDto> SaveGameProfileAsync(int playerId, SavePlayerGameProfileDto dto);

        Task RemoveGameProfileAsync(int playerId, int gameProfileId);
    }
}
