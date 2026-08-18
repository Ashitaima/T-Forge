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
    public class MatchesController : ApiControllerBase
    {
        private readonly IMatchService _matchService;
        private readonly IMatchRosterService _rosterService;
        private readonly ILogger<MatchesController> _logger;

        public MatchesController(
            IMatchService matchService,
            IMatchRosterService rosterService,
            ILogger<MatchesController> logger)
        {
            _matchService = matchService;
            _rosterService = rosterService;
            _logger = logger;
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<MatchDto>>> GetPagedMatches([FromQuery] MatchFilter filter)
        {
            var matches = await _matchService.GetPagedAsync(filter, filter);
            return Ok(matches);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MatchDto>> GetMatch(int id)
        {
            var match = await _matchService.GetByIdAsync(id);
            return Ok(match);
        }

        [HttpGet("{id}/details")]
        public async Task<ActionResult<MatchDto>> GetMatchWithDetails(int id)
        {
            var match = await _matchService.GetWithDetailsAsync(id);
            return Ok(match);
        }

        [HttpGet("scheduled")]
        public async Task<ActionResult<IEnumerable<MatchDto>>> GetScheduledMatches()
        {
            var matches = await _matchService.GetScheduledMatchesAsync();
            return Ok(matches);
        }

        [HttpGet("live")]
        public async Task<ActionResult<IEnumerable<MatchDto>>> GetLiveMatches()
        {
            var matches = await _matchService.GetByStatusAsync(MatchStatus.InProgress);
            return Ok(matches);
        }

        [HttpGet("completed")]
        public async Task<ActionResult<IEnumerable<MatchDto>>> GetCompletedMatches()
        {
            var matches = await _matchService.GetCompletedMatchesAsync();
            return Ok(matches);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<MatchDto>> CreateMatch([FromBody] CreateMatchDto createDto)
        {
            var match = await _matchService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetMatch), new { id = match.Id }, match);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<MatchDto>> UpdateMatch(int id, [FromBody] UpdateMatchDto updateDto)
        {
            var match = await _matchService.UpdateAsync(id, updateDto);
            return Ok(match);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult> DeleteMatch(int id)
        {
            var result = await _matchService.DeleteAsync(id);
            if (!result)
            {
                throw new EntityNotFoundException("Match", id);
            }

            return NoContent();
        }

        [HttpPost("{id}/start")]
        [Authorize]
        public async Task<ActionResult> StartMatch(int id)
        {
            await EnsureCanManageAsync(id);

            var result = await _matchService.StartMatchAsync(id);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося розпочати матч");
            }

            return Ok("Матч успішно розпочато");
        }

        [HttpPost("{id}/complete")]
        [Authorize]
        public async Task<ActionResult> CompleteMatch(int id, [FromBody] CompleteMatchRequest request)
        {
            await EnsureCanManageAsync(id);

            var result = await _matchService.CompleteMatchAsync(
                id, request.WinnerTeamId, request.Result, request.TrackerUrl);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося завершити матч");
            }

            return Ok("Матч успішно завершено");
        }

        /// <summary>Живий рахунок. Зміни розсилаються підписникам через SignalR.</summary>
        [HttpPut("{id}/score")]
        [Authorize]
        public async Task<ActionResult<MatchDto>> UpdateScore(int id, [FromBody] UpdateScoreDto dto)
        {
            await EnsureCanManageAsync(id);

            return Ok(await _matchService.UpdateScoreAsync(id, dto));
        }

        /// <summary>
        /// Зовнішні посилання матчу: трансляція та сторінка в трекері статистики.
        /// Для товариського матчу їх задає капітан — організатора в ньому немає.
        /// </summary>
        [HttpPut("{id}/links")]
        [Authorize]
        public async Task<ActionResult<MatchDto>> UpdateLinks(int id, [FromBody] UpdateLinksRequest request)
        {
            await EnsureCanManageAsync(id);

            return Ok(await _matchService.UpdateLinksAsync(id, request.StreamUrl, request.TrackerUrl));
        }

        /// <summary>
        /// Турнірний матч ведуть організатор і адміністратор; товариський —
        /// ще й капітани обох команд. Рішення ухвалює FriendlyMatchPolicy.
        /// </summary>
        private async Task EnsureCanManageAsync(int matchId)
        {
            var context = await _matchService.GetManageContextAsync(matchId);

            if (!FriendlyMatchPolicy.CanManage(context, GetUserIdOrThrow(), IsAdmin, IsOrganizer))
            {
                throw new ForbiddenException(FriendlyMatchPolicy.IsFriendly(context)
                    ? "Вести товариський матч можуть лише капітани команд-учасниць"
                    : "Вести турнірний матч може лише організатор");
            }
        }

        // ---- Ростер матчу ----

        [HttpGet("{id}/players")]
        public async Task<ActionResult<IEnumerable<MatchPlayerDto>>> GetRoster(int id)
        {
            return Ok(await _rosterService.GetRosterAsync(id));
        }

        /// <summary>Підтягує активних гравців обох команд у ростер.</summary>
        [HttpPost("{id}/players/autofill")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<IEnumerable<MatchPlayerDto>>> AutoFillRoster(int id)
        {
            return Ok(await _rosterService.AutoFillAsync(id));
        }

        [HttpPost("{id}/players")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<MatchPlayerDto>> AddRosterPlayer(int id, [FromBody] CreateMatchPlayerDto dto)
        {
            return Ok(await _rosterService.AddPlayerAsync(id, dto));
        }

        [HttpPut("{id}/players/{entryId}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<MatchPlayerDto>> UpdateRosterPlayer(
            int id, int entryId, [FromBody] UpdateMatchPlayerDto dto)
        {
            return Ok(await _rosterService.UpdateEntryAsync(id, entryId, dto));
        }

        [HttpDelete("{id}/players/{entryId}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult> RemoveRosterPlayer(int id, int entryId)
        {
            await _rosterService.RemoveEntryAsync(id, entryId);
            return NoContent();
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult> CancelMatch(int id, [FromBody] CancelMatchRequest request)
        {
            var result = await _matchService.CancelMatchAsync(id, request.Reason);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося скасувати матч");
            }

            return Ok("Матч успішно скасовано");
        }
    }

    public class CompleteMatchRequest
    {
        public int? WinnerTeamId { get; set; }
        public string? Result { get; set; }

        /// <summary>Необовʼязкове посилання на матч у трекері статистики.</summary>
        public string? TrackerUrl { get; set; }
    }

    public class UpdateLinksRequest
    {
        public string? StreamUrl { get; set; }
        public string? TrackerUrl { get; set; }
    }

    public class CancelMatchRequest
    {
        public string? Reason { get; set; }
    }
}
