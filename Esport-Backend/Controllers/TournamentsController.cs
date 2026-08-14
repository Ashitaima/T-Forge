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
    public class TournamentsController : ApiControllerBase
    {
        private readonly ITournamentService _tournamentService;
        private readonly IBracketService _bracketService;
        private readonly ILogger<TournamentsController> _logger;

        public TournamentsController(
            ITournamentService tournamentService,
            IBracketService bracketService,
            ILogger<TournamentsController> logger)
        {
            _tournamentService = tournamentService;
            _bracketService = bracketService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TournamentDto>>> GetTournaments()
        {
            var tournaments = await _tournamentService.GetAllActiveAsync();
            return Ok(tournaments);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<TournamentDto>>> GetPagedTournaments([FromQuery] TournamentFilter filter)
        {
            var tournaments = await _tournamentService.GetPagedAsync(filter);
            return Ok(tournaments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TournamentDto>> GetTournament(int id)
        {
            var tournament = await _tournamentService.GetWithMatchesAsync(id);
            return Ok(tournament);
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<TournamentDto>>> GetTournamentsByStatus(string status)
        {
            var tournaments = await _tournamentService.GetByStatusAsync(status);
            return Ok(tournaments);
        }

        [HttpGet("stats")]
        public async Task<ActionResult<TournamentStatsDto>> GetTournamentStats()
        {
            var stats = await _tournamentService.GetStatsAsync();
            return Ok(stats);
        }

        [HttpPost]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<TournamentDto>> CreateTournament([FromBody] CreateTournamentDto createDto)
        {
            var tournament = await _tournamentService.CreateAsync(createDto, ResolveOwnerId(createDto.OrganizerId));
            return CreatedAtAction(nameof(GetTournament), new { id = tournament.Id }, tournament);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<TournamentDto>> UpdateTournament(int id, [FromBody] UpdateTournamentDto updateDto)
        {
            var tournament = await _tournamentService.UpdateAsync(id, updateDto);
            return Ok(tournament);
        }

        // ---- Реєстрація команд ----

        [HttpGet("{id}/teams")]
        public async Task<ActionResult<IEnumerable<TeamSummaryDto>>> GetRegisteredTeams(int id)
        {
            var teams = await _tournamentService.GetRegisteredTeamsAsync(id);
            return Ok(teams);
        }

        [HttpPost("{id}/teams/{teamId}")]
        [Authorize]
        public async Task<ActionResult> RegisterTeam(int id, int teamId)
        {
            await _tournamentService.RegisterTeamAsync(id, teamId, GetUserIdOrThrow(), IsAdmin);
            return Ok(new { message = "Команду зареєстровано на турнір" });
        }

        [HttpDelete("{id}/teams/{teamId}")]
        [Authorize]
        public async Task<ActionResult> WithdrawTeam(int id, int teamId)
        {
            await _tournamentService.WithdrawTeamAsync(id, teamId, GetUserIdOrThrow(), IsAdmin);
            return Ok(new { message = "Команду знято з турніру" });
        }

        // ---- Турнірна сітка ----

        [HttpPost("{id}/bracket/generate")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult> GenerateBracket(int id)
        {
            var created = await _bracketService.GenerateAsync(id);
            return Ok(new { message = $"Сітку створено: {created} матчів у першому раунді", matches = created });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTournament(int id)
        {
            await _tournamentService.DeleteAsync(id);
            return NoContent();
        }
    }
}
