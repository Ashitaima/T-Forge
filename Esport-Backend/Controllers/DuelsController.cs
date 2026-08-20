using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.Common;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    /// <summary>
    /// Дуелі 1 на 1.
    ///
    /// Ролей тут немає: дуель не має ні організатора, ні капітана. Хто що
    /// може — вирішує Common/DuelPolicy.cs, і кожна дія, що змінює дуель,
    /// проходить через нього.
    /// </summary>
    [ApiController]
    [Route("api/duels")]
    public class DuelsController : ApiControllerBase
    {
        private readonly IDuelService _duelService;

        public DuelsController(IDuelService duelService)
        {
            _duelService = duelService;
        }

        /// <summary>Читання публічне — як і решта підсумків у проєкті.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DuelDto>>> GetDuels([FromQuery] int? playerId)
        {
            return Ok(await _duelService.GetAllAsync(playerId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DuelDto>> GetDuel(int id)
        {
            return Ok(await _duelService.GetByIdAsync(id)
                ?? throw new EntityNotFoundException("Duel", id));
        }

        [HttpGet("record/{playerId}")]
        public async Task<ActionResult<DuelRecordDto>> GetRecord(int playerId)
        {
            return Ok(await _duelService.GetRecordAsync(playerId));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<DuelDto>> CreateDuel([FromBody] CreateDuelDto createDto)
        {
            var duel = await _duelService.CreateAsync(createDto, GetUserIdOrThrow());
            return CreatedAtAction(nameof(GetDuel), new { id = duel.Id }, duel);
        }

        [HttpPost("{id}/respond")]
        [Authorize]
        public async Task<ActionResult<DuelDto>> Respond(int id, [FromQuery] bool accept)
        {
            await EnsureAsync(id, DuelPolicy.CanRespond,
                "Відповісти може лише той, кого викликали; відкритий виклик — будь-хто, крім автора");

            return Ok(await _duelService.RespondAsync(id, accept, GetUserIdOrThrow()));
        }

        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult<DuelDto>> Cancel(int id)
        {
            await EnsureAsync(id, DuelPolicy.CanCancel,
                "Скасувати виклик може лише той, хто його надіслав, і лише до відповіді");

            return Ok(await _duelService.CancelAsync(id));
        }

        [HttpPost("{id}/start")]
        [Authorize]
        public async Task<ActionResult<DuelDto>> Start(int id)
        {
            await EnsureAsync(id, DuelPolicy.CanManage, ManageDenied);

            return Ok(await _duelService.StartAsync(id));
        }

        [HttpPost("{id}/complete")]
        [Authorize]
        public async Task<ActionResult<DuelDto>> Complete(int id, [FromBody] CompleteDuelDto dto)
        {
            await EnsureAsync(id, DuelPolicy.CanManage, ManageDenied);

            return Ok(await _duelService.CompleteAsync(id, dto));
        }

        private const string ManageDenied =
            "Вести дуель можуть лише її учасники, і лише поки вона не завершена";

        /// <summary>
        /// Кожна дія читає той самий контекст і питає DuelPolicy — щоб жодна
        /// з них не завела власного уявлення про те, кому можна.
        /// </summary>
        private async Task EnsureAsync(
            int id,
            Func<DuelPolicy.Context, int, bool, bool> rule,
            string message)
        {
            var context = await _duelService.GetPolicyContextAsync(id);

            if (!rule(context, GetUserIdOrThrow(), IsAdmin))
            {
                throw new ForbiddenException(message);
            }
        }
    }
}
