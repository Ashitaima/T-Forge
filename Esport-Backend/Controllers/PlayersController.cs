using TForge.DTOs;
using TForge.Services.Interfaces;
using TForge.Common;
using TForge.Common.Filters;
using TForge.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace TForge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ApiControllerBase
    {
        private readonly IPlayerService _playerService;
        private readonly ILogger<PlayersController> _logger;

        public PlayersController(IPlayerService playerService, ILogger<PlayersController> logger)
        {
            _playerService = playerService;
            _logger = logger;
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<PlayerDto>>> GetPagedPlayers([FromQuery] PlayerFilter filter)
        {
            var players = await _playerService.GetPagedAsync(filter, filter);
            return Ok(players);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PlayerDto>> GetPlayer(int id)
        {
            var player = await _playerService.GetByIdAsync(id);
            return Ok(player);
        }

        [HttpGet("{id}/team")]
        public async Task<ActionResult<PlayerDto>> GetPlayerWithTeam(int id)
        {
            var player = await _playerService.GetWithTeamAsync(id);
            return Ok(player);
        }

        /// <summary>Профіль гравця: особисті дані та статистика за всіма матчами.</summary>
        [HttpGet("{id}/profile")]
        public async Task<ActionResult<PlayerProfileDto>> GetPlayerProfile(int id)
        {
            return Ok(await _playerService.GetProfileAsync(id));
        }

        /// <summary>Журнал матчів гравця з його власною статистикою за кожен матч.</summary>
        [HttpGet("{id}/matches")]
        public async Task<ActionResult<PagedResponse<PlayerMatchDto>>> GetPlayerMatches(
            int id, [FromQuery] PagedRequest request)
        {
            return Ok(await _playerService.GetMatchLogAsync(id, request));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PlayerDto>> CreatePlayer([FromBody] CreatePlayerDto createDto)
        {
            var player = await _playerService.CreateAsync(createDto, ResolveOwnerId(createDto.UserId));
            return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, player);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<PlayerDto>> UpdatePlayer(int id, [FromBody] UpdatePlayerDto updateDto)
        {
            if (!await IsOwnerOrAdminAsync(id))
            {
                return Forbid();
            }

            var player = await _playerService.UpdateAsync(id, updateDto);
            return Ok(player);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeletePlayer(int id)
        {
            if (!await IsOwnerOrAdminAsync(id))
            {
                return Forbid();
            }

            var result = await _playerService.DeleteAsync(id);
            if (!result)
            {
                throw new EntityNotFoundException("Player", id);
            }

            return NoContent();
        }

        [HttpPost("{playerId}/join-team/{teamId}")]
        [Authorize]
        public async Task<ActionResult> JoinTeam(int playerId, int teamId)
        {
            if (!await IsOwnerOrAdminAsync(playerId))
            {
                return Forbid();
            }

            var result = await _playerService.JoinTeamAsync(playerId, teamId);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося приєднатися до команди");
            }

            return Ok("Гравець успішно приєднався до команди");
        }

        [HttpPost("{playerId}/leave-team")]
        [Authorize]
        public async Task<ActionResult> LeaveTeam(int playerId)
        {
            if (!await IsOwnerOrAdminAsync(playerId))
            {
                return Forbid();
            }

            var result = await _playerService.LeaveTeamAsync(playerId);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося покинути команду");
            }

            return Ok("Гравець успішно покинув команду");
        }

        /// <summary>
        /// Профіль гравця може змінювати лише його власник або адміністратор.
        /// </summary>
        private async Task<bool> IsOwnerOrAdminAsync(int playerId)
        {
            if (IsAdmin)
            {
                return true;
            }

            var player = await _playerService.GetByIdAsync(playerId);
            if (player == null)
            {
                throw new EntityNotFoundException("Player", playerId);
            }

            return player.UserId == GetUserIdOrThrow();
        }

    }
}
