using Computational_Practice.DTOs;
using Computational_Practice.Services.Interfaces;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;
using Computational_Practice.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Computational_Practice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
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

        [HttpGet("{id}/matches")]
        public async Task<ActionResult<PlayerDto>> GetPlayerWithMatches(int id)
        {
            var player = await _playerService.GetWithMatchesAsync(id);
            return Ok(player);
        }

        [HttpPost]
        public async Task<ActionResult<PlayerDto>> CreatePlayer([FromBody] CreatePlayerDto createDto)
        {
            var player = await _playerService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, player);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PlayerDto>> UpdatePlayer(int id, [FromBody] UpdatePlayerDto updateDto)
        {
            var player = await _playerService.UpdateAsync(id, updateDto);
            return Ok(player);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePlayer(int id)
        {
            var result = await _playerService.DeleteAsync(id);
            if (!result)
            {
                throw new EntityNotFoundException("Player", id);
            }

            return NoContent();
        }

        [HttpPost("{playerId}/join-team/{teamId}")]
        public async Task<ActionResult> JoinTeam(int playerId, int teamId)
        {
            var result = await _playerService.JoinTeamAsync(playerId, teamId);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося приєднатися до команди");
            }

            return Ok("Гравець успішно приєднався до команди");
        }

        [HttpPost("{playerId}/leave-team")]
        public async Task<ActionResult> LeaveTeam(int playerId)
        {
            var result = await _playerService.LeaveTeamAsync(playerId);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося покинути команду");
            }

            return Ok("Гравець успішно покинув команду");
        }
    }
}
