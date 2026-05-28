using Computational_Practice.DTOs;
using Computational_Practice.Services.Interfaces;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;
using Computational_Practice.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Computational_Practice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly ILogger<TeamsController> _logger;

        public TeamsController(ITeamService teamService, ILogger<TeamsController> logger)
        {
            _teamService = teamService;
            _logger = logger;
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<TeamDto>>> GetPagedTeams([FromQuery] TeamFilter filter)
        {
            var teams = await _teamService.GetPagedAsync(filter, filter);
            return Ok(teams);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TeamDto>> GetTeam(int id)
        {
            var team = await _teamService.GetByIdAsync(id);
            return Ok(team);
        }

        [HttpGet("{id}/players")]
        public async Task<ActionResult<TeamDto>> GetTeamWithPlayers(int id)
        {
            var team = await _teamService.GetWithPlayersAsync(id);
            return Ok(team);
        }

        [HttpPost]
        public async Task<ActionResult<TeamDto>> CreateTeam([FromBody] CreateTeamDto createDto)
        {
            var team = await _teamService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<TeamDto>> UpdateTeam(int id, [FromBody] UpdateTeamDto updateDto)
        {
            var userId = GetUserIdOrThrow();
            var existingTeam = await _teamService.GetWithPlayersAsync(id);
            if (existingTeam == null)
            {
                throw new EntityNotFoundException("Team", id);
            }

            if (existingTeam.Captain?.Id != userId)
            {
                return Forbid();
            }

            var updatedTeam = await _teamService.UpdateAsync(id, updateDto);
            return Ok(updatedTeam);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteTeam(int id)
        {
            var userId = GetUserIdOrThrow();
            var team = await _teamService.GetWithPlayersAsync(id);
            if (team == null)
            {
                throw new EntityNotFoundException("Team", id);
            }

            if (team.Captain?.Id != userId)
            {
                return Forbid();
            }

            var result = await _teamService.DeleteAsync(id);
            if (!result)
            {
                throw new EntityNotFoundException("Team", id);
            }

            return NoContent();
        }

        [HttpPost("{teamId}/players/{playerId}")]
        [Authorize]
        public async Task<ActionResult> AddPlayerToTeam(int teamId, int playerId)
        {
            var userId = GetUserIdOrThrow();
            var team = await _teamService.GetWithPlayersAsync(teamId);
            if (team == null)
            {
                throw new EntityNotFoundException("Team", teamId);
            }

            if (team.Captain?.Id != userId)
            {
                return Forbid();
            }

            var result = await _teamService.AddPlayerToTeamAsync(teamId, playerId);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося додати гравця до команди");
            }

            return Ok("Гравця успішно додано до команди");
        }

        [HttpDelete("{teamId}/players/{playerId}")]
        [Authorize]
        public async Task<ActionResult> RemovePlayerFromTeam(int teamId, int playerId)
        {
            var userId = GetUserIdOrThrow();
            var team = await _teamService.GetWithPlayersAsync(teamId);
            if (team == null)
            {
                throw new EntityNotFoundException("Team", teamId);
            }

            if (team.Captain?.Id != userId)
            {
                return Forbid();
            }

            var result = await _teamService.RemovePlayerFromTeamAsync(teamId, playerId);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося видалити гравця з команди");
            }

            return Ok("Гравця успішно видалено з команди");
        }

        private int GetUserIdOrThrow()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                throw new BusinessLogicException("Некоректний токен");
            }

            return userId;
        }
    }
}
