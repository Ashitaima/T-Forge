using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    [ApiController]
    [Route("api/tournament-invitations")]
    public class TournamentInvitationsController : ApiControllerBase
    {
        private readonly ITournamentInvitationService _invitationService;

        public TournamentInvitationsController(ITournamentInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        [HttpPost("{id}/accept")]
        [Authorize]
        public async Task<ActionResult<TournamentInvitationDto>> Accept(int id)
        {
            return Ok(await _invitationService.AcceptAsync(id, GetUserIdOrThrow(), IsAdmin));
        }

        [HttpPost("{id}/decline")]
        [Authorize]
        public async Task<ActionResult<TournamentInvitationDto>> Decline(int id)
        {
            return Ok(await _invitationService.DeclineAsync(id, GetUserIdOrThrow(), IsAdmin));
        }

        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult<TournamentInvitationDto>> Cancel(int id)
        {
            return Ok(await _invitationService.CancelAsync(id, GetUserIdOrThrow(), IsAdmin));
        }
    }
}
