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
        public async Task<ActionResult<TeamDto>> UpdateTeam(int id, [FromBody] UpdateTeamDto updateDto)
        {
            var team = await _teamService.UpdateAsync(id, updateDto);
            return Ok(team);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTeam(int id)
        {
            var result = await _teamService.DeleteAsync(id);
            if (!result)
            {
                throw new EntityNotFoundException("Team", id);
            }

            return NoContent();
        }

        [HttpPost("{teamId}/players/{playerId}")]
        public async Task<ActionResult> AddPlayerToTeam(int teamId, int playerId)
        {
            var result = await _teamService.AddPlayerToTeamAsync(teamId, playerId);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося додати гравця до команди");
            }

            return Ok("Гравця успішно додано до команди");
        }

        [HttpDelete("{teamId}/players/{playerId}")]
        public async Task<ActionResult> RemovePlayerFromTeam(int teamId, int playerId)
        {
            var result = await _teamService.RemovePlayerFromTeamAsync(teamId, playerId);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося видалити гравця з команди");
            }

            return Ok("Гравця успішно видалено з команди");
        }
    }
}
