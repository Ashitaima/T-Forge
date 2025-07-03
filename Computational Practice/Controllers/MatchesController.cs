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
    public class MatchesController : ControllerBase
    {
        private readonly IMatchService _matchService;
        private readonly ILogger<MatchesController> _logger;

        public MatchesController(IMatchService matchService, ILogger<MatchesController> logger)
        {
            _matchService = matchService;
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
            var matches = await _matchService.GetByStatusAsync("InProgress");
            return Ok(matches);
        }

        [HttpGet("completed")]
        public async Task<ActionResult<IEnumerable<MatchDto>>> GetCompletedMatches()
        {
            var matches = await _matchService.GetCompletedMatchesAsync();
            return Ok(matches);
        }

        [HttpPost]
        public async Task<ActionResult<MatchDto>> CreateMatch([FromBody] CreateMatchDto createDto)
        {
            var match = await _matchService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetMatch), new { id = match.Id }, match);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<MatchDto>> UpdateMatch(int id, [FromBody] UpdateMatchDto updateDto)
        {
            var match = await _matchService.UpdateAsync(id, updateDto);
            return Ok(match);
        }

        [HttpDelete("{id}")]
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
        public async Task<ActionResult> StartMatch(int id)
        {
            var result = await _matchService.StartMatchAsync(id);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося розпочати матч");
            }

            return Ok("Матч успішно розпочато");
        }

        [HttpPost("{id}/complete")]
        public async Task<ActionResult> CompleteMatch(int id, [FromBody] CompleteMatchRequest request)
        {
            var result = await _matchService.CompleteMatchAsync(id, request.WinnerTeamId, request.Result);
            if (!result)
            {
                throw new BusinessLogicException("Не вдалося завершити матч");
            }

            return Ok("Матч успішно завершено");
        }

        [HttpPost("{id}/cancel")]
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
    }

    public class CancelMatchRequest
    {
        public string? Reason { get; set; }
    }
}
