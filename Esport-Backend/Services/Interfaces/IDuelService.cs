using TForge.Common;
using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IDuelService
    {
        /// <summary>Усі дуелі; playerId звужує до тих, у яких гравець бере участь.</summary>
        Task<IEnumerable<DuelDto>> GetAllAsync(int? playerId = null);

        Task<DuelDto?> GetByIdAsync(int id);

        /// <summary>Рахунок гравця в дуелях — окремий від показників матчів.</summary>
        Task<DuelRecordDto> GetRecordAsync(int playerId);

        /// <summary>Ініціатора визначає сервер за токеном, а не клієнт.</summary>
        Task<DuelDto> CreateAsync(CreateDuelDto createDto, int requestingUserId);

        /// <summary>Дані, потрібні DuelPolicy, щоб вирішити, хто що може.</summary>
        Task<DuelPolicy.Context> GetPolicyContextAsync(int id);

        /// <summary>Прийняти відкритий виклик — це й означає назватися суперником.</summary>
        Task<DuelDto> RespondAsync(int id, bool accept, int requestingUserId);
        Task<DuelDto> CancelAsync(int id);
        Task<DuelDto> StartAsync(int id);
        Task<DuelDto> CompleteAsync(int id, CompleteDuelDto completeDto);
    }
}
