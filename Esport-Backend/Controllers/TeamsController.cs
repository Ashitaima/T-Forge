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
    public class TeamsController : ApiControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IStandingsService _standingsService;
        private readonly IMembershipRequestService _membershipRequestService;
        private readonly ILogger<TeamsController> _logger;

        public TeamsController(
            ITeamService teamService,
            IStandingsService standingsService,
            IMembershipRequestService membershipRequestService,
            ILogger<TeamsController> logger)
        {
            _teamService = teamService;
            _standingsService = standingsService;
            _membershipRequestService = membershipRequestService;
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
            var team = await _teamService.GetByIdAsync(id) ?? throw new EntityNotFoundException("Team", id);
            return Ok(team);
        }

        [HttpGet("{id}/players")]
        public async Task<ActionResult<TeamDto>> GetTeamWithPlayers(int id)
        {
            var team = await _teamService.GetWithPlayersAsync(id) ?? throw new EntityNotFoundException("Team", id);
            return Ok(team);
        }

        /// <summary>Форма команди за всіма зіграними матчами.</summary>
        [HttpGet("{id}/summary")]
        public async Task<ActionResult<TeamSummaryStatsDto>> GetTeamSummary(int id)
        {
            return Ok(await _standingsService.GetTeamSummaryAsync(id));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<TeamDto>> CreateTeam([FromBody] CreateTeamDto createDto)
        {
            var team = await _teamService.CreateAsync(createDto, ResolveOwnerId(createDto.CaptainId));
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

        /// <summary>
        /// Примусове додавання гравця — лише для адміністрування.
        /// Капітан додає гравців через запрошення, а не напряму.
        /// </summary>
        [HttpPost("{teamId}/players/{playerId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AddPlayerToTeam(int teamId, int playerId)
        {
            _ = await _teamService.GetByIdAsync(teamId)
                ?? throw new EntityNotFoundException("Team", teamId);

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

        /// <summary>Капітан запрошує гравця до команди.</summary>
        [HttpPost("{teamId}/invitations/{playerId}")]
        [Authorize]
        public async Task<ActionResult<MembershipRequestDto>> InvitePlayer(int teamId, int playerId)
        {
            var request = await _membershipRequestService.InviteAsync(
                teamId, playerId, GetUserIdOrThrow(), IsAdmin);

            return Ok(request);
        }

        /// <summary>Запити команди — і вхідні заявки, і надіслані запрошення.</summary>
        [HttpGet("{teamId}/membership-requests")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MembershipRequestDto>>> GetMembershipRequests(
            int teamId, [FromQuery] string? status)
        {
            var requests = await _membershipRequestService.GetForTeamAsync(
                teamId, status, GetUserIdOrThrow(), IsAdmin);

            return Ok(requests);
        }

    }
}
