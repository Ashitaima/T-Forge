using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    /// <summary>
    /// Заявки на роль організатора. Роль дає право створювати турніри, тож
    /// видає її лише адміністратор — див. Common/OrganizerRequestPolicy.cs.
    /// </summary>
    [ApiController]
    [Route("api/organizer-requests")]
    public class OrganizerRequestsController : ApiControllerBase
    {
        private readonly IOrganizerRequestService _requestService;

        public OrganizerRequestsController(IOrganizerRequestService requestService)
        {
            _requestService = requestService;
        }

        /// <summary>Гравець просить роль організатора.</summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<OrganizerRequestDto>> Apply(
            [FromBody] CreateOrganizerRequestDto createDto)
        {
            return Ok(await _requestService.ApplyAsync(GetUserIdOrThrow(), createDto));
        }

        /// <summary>Власні заявки — щоб заявник бачив, на якій вони стадії.</summary>
        [HttpGet("mine")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrganizerRequestDto>>> GetMine()
        {
            return Ok(await _requestService.GetForUserAsync(GetUserIdOrThrow()));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrganizerRequestDto>>> GetAll(
            [FromQuery] string? status)
        {
            return Ok(await _requestService.GetAllAsync(status));
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<OrganizerRequestDto>> Approve(int id)
        {
            return Ok(await _requestService.ApproveAsync(id, GetUserIdOrThrow(), IsAdmin));
        }

        [HttpPost("{id}/decline")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<OrganizerRequestDto>> Decline(
            int id, [FromBody] RespondOrganizerRequestDto? respondDto = null)
        {
            return Ok(await _requestService.DeclineAsync(
                id, GetUserIdOrThrow(), IsAdmin, respondDto ?? new RespondOrganizerRequestDto()));
        }

        /// <summary>Заявник відкликає власну заявку, поки її не розглянуто.</summary>
        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult<OrganizerRequestDto>> Cancel(int id)
        {
            return Ok(await _requestService.CancelAsync(id, GetUserIdOrThrow()));
        }
    }
}
