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
        private readonly IMatchChallengeService _matchChallengeService;
        private readonly ITournamentInvitationService _tournamentInvitationService;
        private readonly ILogger<TeamsController> _logger;

        public TeamsController(
            ITeamService teamService,
            IStandingsService standingsService,
            IMembershipRequestService membershipRequestService,
            IMatchChallengeService matchChallengeService,
            ITournamentInvitationService tournamentInvitationService,
            ILogger<TeamsController> logger)
        {
            _teamService = teamService;
            _standingsService = standingsService;
            _membershipRequestService = membershipRequestService;
            _matchChallengeService = matchChallengeService;
            _tournamentInvitationService = tournamentInvitationService;
            _logger = logger;
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<TeamRowDto>>> GetPagedTeams([FromQuery] TeamFilter filter)
        {
            return Ok(await _teamService.GetPagedRowsAsync(filter, filter));
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

        /// <summary>
        /// Команду може створити будь-який авторизований користувач — він стає
        /// її капітаном. Капітана визначає ResolveOwnerId за токеном, тож чужий
        /// id може підставити лише адміністратор.
        /// </summary>
        [HttpPost]
        [Authorize]
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

            // Адміністратор стоїть над перевіркою власності, як і всюди:
            // без цього він не міг виправити навіть друкарську помилку в назві.
            if (!IsAdmin && existingTeam.Captain?.Id != userId)
            {
                return Forbid();
            }

            var updatedTeam = await _teamService.UpdateAsync(id, updateDto);
            return Ok(updatedTeam);
        }

        /// <summary>
        /// Передача капітанства. Капітанство — це колонка, а не роль, тож
        /// перевірку робить TeamCaptaincyPolicy у сервісі, а не [Authorize].
        /// </summary>
        [HttpPut("{id}/captain")]
        [Authorize]
        public async Task<ActionResult<TeamDto>> TransferCaptaincy(
            int id, [FromBody] TransferCaptaincyDto transferDto)
        {
            var team = await _teamService.TransferCaptaincyAsync(
                id, transferDto.PlayerId, GetUserIdOrThrow(), IsAdmin);

            return Ok(team);
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

            if (!IsAdmin && team.Captain?.Id != userId)
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

        /// <summary>
        /// Виклики команди — і надіслані, і отримані. Читання публічне, як і решта
        /// даних про матчі: сам факт виклику не є приватною інформацією.
        /// </summary>
        [HttpGet("{teamId}/match-challenges")]
        public async Task<ActionResult<IEnumerable<MatchChallengeDto>>> GetMatchChallenges(
            int teamId, [FromQuery] string? status)
        {
            return Ok(await _matchChallengeService.GetForTeamAsync(teamId, status));
        }

        /// <summary>
        /// Запрошення та заявки команди на турніри. Читання публічне — з того
        /// самого міркування, що й виклики на матч.
        /// </summary>
        [HttpGet("{teamId}/tournament-invitations")]
        public async Task<ActionResult<IEnumerable<TournamentInvitationDto>>> GetTournamentInvitations(
            int teamId, [FromQuery] string? status)
        {
            return Ok(await _tournamentInvitationService.GetForTeamAsync(teamId, status));
        }
    }
}
