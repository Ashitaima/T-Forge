using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    [ApiController]
    [Route("api/match-challenges")]
    public class MatchChallengesController : ApiControllerBase
    {
        private readonly IMatchChallengeService _challengeService;

        public MatchChallengesController(IMatchChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        /// <summary>Капітан викликає іншу команду на товариський матч.</summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<MatchChallengeDto>> Create([FromBody] CreateMatchChallengeDto createDto)
        {
            var challenge = await _challengeService.CreateAsync(createDto, GetUserIdOrThrow(), IsAdmin);
            return Ok(challenge);
        }

        [HttpPost("{id}/accept")]
        [Authorize]
        public async Task<ActionResult<MatchChallengeDto>> Accept(int id)
        {
            return Ok(await _challengeService.AcceptAsync(id, GetUserIdOrThrow(), IsAdmin));
        }

        [HttpPost("{id}/decline")]
        [Authorize]
        public async Task<ActionResult<MatchChallengeDto>> Decline(int id)
        {
            return Ok(await _challengeService.DeclineAsync(id, GetUserIdOrThrow(), IsAdmin));
        }

        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult<MatchChallengeDto>> Cancel(int id)
        {
            return Ok(await _challengeService.CancelAsync(id, GetUserIdOrThrow(), IsAdmin));
        }

        /// <summary>Виклики, що чекають на відповідь поточного користувача.</summary>
        [HttpGet("pending")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MatchChallengeDto>>> GetPending()
        {
            return Ok(await _challengeService.GetPendingForUserAsync(GetUserIdOrThrow()));
        }
    }
}
