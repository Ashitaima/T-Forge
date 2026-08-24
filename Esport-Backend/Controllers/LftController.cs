using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.Common;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    /// <summary>
    /// Стрічки пошуку складу. Усе під [Authorize]: змах — це згода, а згоду
    /// анонім дати не може. Гравця беремо з токена, ніколи з тіла запиту.
    /// </summary>
    [ApiController]
    [Route("api/lft")]
    [Authorize]
    public class LftController : ApiControllerBase
    {
        private readonly ILftService _service;

        public LftController(ILftService service) => _service = service;

        [HttpGet("openings")]
        public async Task<IActionResult> Openings([FromQuery] PagedRequest request) =>
            Ok(await _service.GetOpeningFeedAsync(request, GetUserIdOrThrow()));

        [HttpGet("candidates")]
        public async Task<IActionResult> Candidates(
            [FromQuery] int openingId, [FromQuery] PagedRequest request) =>
            Ok(await _service.GetCandidateFeedAsync(openingId, request, GetUserIdOrThrow(), IsAdmin));

        [HttpPost("swipe")]
        public async Task<IActionResult> Swipe([FromBody] SwipeDto dto) =>
            Ok(await _service.SwipeAsync(dto, GetUserIdOrThrow(), IsAdmin));
    }
}
