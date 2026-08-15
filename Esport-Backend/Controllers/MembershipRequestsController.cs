using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    [ApiController]
    [Route("api/membership-requests")]
    public class MembershipRequestsController : ApiControllerBase
    {
        private readonly IMembershipRequestService _membershipRequestService;

        public MembershipRequestsController(IMembershipRequestService membershipRequestService)
        {
            _membershipRequestService = membershipRequestService;
        }

        [HttpPost("{id}/accept")]
        [Authorize]
        public async Task<ActionResult<MembershipRequestDto>> Accept(int id)
        {
            return Ok(await _membershipRequestService.AcceptAsync(id, GetUserIdOrThrow(), IsAdmin));
        }

        [HttpPost("{id}/decline")]
        [Authorize]
        public async Task<ActionResult<MembershipRequestDto>> Decline(int id)
        {
            return Ok(await _membershipRequestService.DeclineAsync(id, GetUserIdOrThrow(), IsAdmin));
        }

        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult<MembershipRequestDto>> Cancel(int id)
        {
            return Ok(await _membershipRequestService.CancelAsync(id, GetUserIdOrThrow(), IsAdmin));
        }
    }
}
