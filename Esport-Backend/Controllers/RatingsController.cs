using Microsoft.AspNetCore.Mvc;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    /// <summary>
    /// Рейтингова драбина. Читання публічне — як і решта підсумків, профілів
    /// і таблиць: рейтинг має сенс саме тому, що його видно всім.
    /// </summary>
    [ApiController]
    [Route("api/ratings")]
    public class RatingsController : ApiControllerBase
    {
        /// <summary>Скільки останніх матчів показує графік за замовчуванням.</summary>
        private const int DefaultHistoryLength = 20;

        private readonly IRatingService _ratingService;

        public RatingsController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        [HttpGet("teams/{teamId}")]
        public async Task<ActionResult<IEnumerable<RatingDto>>> GetTeamRatings(int teamId)
        {
            return Ok(await _ratingService.GetTeamRatingsAsync(teamId));
        }

        [HttpGet("players/{playerId}")]
        public async Task<ActionResult<IEnumerable<RatingDto>>> GetPlayerRatings(int playerId)
        {
            return Ok(await _ratingService.GetPlayerRatingsAsync(playerId));
        }

        [HttpGet("teams/{teamId}/history")]
        public async Task<ActionResult<IEnumerable<RatingChangeDto>>> GetTeamHistory(
            int teamId, [FromQuery] string? game, [FromQuery] int take = DefaultHistoryLength)
        {
            return Ok(await _ratingService.GetTeamHistoryAsync(teamId, game, take));
        }

        [HttpGet("players/{playerId}/history")]
        public async Task<ActionResult<IEnumerable<RatingChangeDto>>> GetPlayerHistory(
            int playerId, [FromQuery] string? game, [FromQuery] int take = DefaultHistoryLength)
        {
            return Ok(await _ratingService.GetPlayerHistoryAsync(playerId, game, take));
        }

        /// <summary>Зміна рейтингу обох команд у матчі — «+18» на картці матчу.</summary>
        [HttpGet("matches/{matchId}")]
        public async Task<ActionResult<MatchRatingDeltaDto>> GetMatchDelta(int matchId)
        {
            return Ok(await _ratingService.GetMatchDeltaAsync(matchId));
        }
    }
}
