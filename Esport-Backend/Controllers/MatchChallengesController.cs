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

        /// <summary>
        /// Приймає виклик. Тіло потрібне лише для відкритого виклику й лише
        /// тоді, коли капітан веде кілька команд — інакше команду визначає
        /// сервер.
        /// </summary>
        [HttpPost("{id}/accept")]
        [Authorize]
        public async Task<ActionResult<MatchChallengeDto>> Accept(
            int id, [FromBody] AcceptMatchChallengeDto? acceptDto = null)
        {
            return Ok(await _challengeService.AcceptAsync(
                id, GetUserIdOrThrow(), IsAdmin, acceptDto?.TeamId));
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

        /// <summary>
        /// Відкриті виклики — ті, у яких суперника ще не названо. Прийняти
        /// може капітан будь-якої іншої команди, тож список загальний.
        /// </summary>
        [HttpGet("open")]
        public async Task<ActionResult<IEnumerable<MatchChallengeDto>>> GetOpen([FromQuery] string? game)
        {
            return Ok(await _challengeService.GetOpenAsync(game));
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
