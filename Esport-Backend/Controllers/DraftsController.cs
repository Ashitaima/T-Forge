using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.Common;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    /// <summary>
    /// Драфт матчу. Роль тут майже нічого не вирішує — вирішує
    /// Common/DraftPolicy.cs, і в ньому два різні права: ходити може лише
    /// капітан, чий зараз хід, а керувати драфтом — ще й організатор із
    /// адміністратором.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class DraftsController : ApiControllerBase
    {
        private readonly IDraftService _draftService;

        public DraftsController(IDraftService draftService)
        {
            _draftService = draftService;
        }

        /// <summary>
        /// Читання відкрите, як і решта відомостей про матч. Відсутній драфт
        /// віддає 404, а не Ok(null): той серіалізується в 204, який клієнт
        /// читає як успіх.
        /// </summary>
        [HttpGet("matches/{matchId}/draft")]
        public async Task<ActionResult<DraftDto>> GetForMatch(int matchId)
        {
            return Ok(await _draftService.GetForMatchAsync(matchId)
                ?? throw new EntityNotFoundException("Драфту для цього матчу немає"));
        }

        [HttpPost("matches/{matchId}/draft")]
        [Authorize]
        public async Task<ActionResult<DraftDto>> Create(int matchId, [FromBody] CreateDraftDto dto)
        {
            var context = await _draftService.GetPolicyContextForMatchAsync(matchId);
            EnsureCanManage(context);

            return Ok(await _draftService.CreateAsync(matchId, dto.TurnSeconds));
        }

        [HttpPost("drafts/{id}/start")]
        [Authorize]
        public async Task<ActionResult<DraftDto>> Start(int id)
        {
            var (context, _) = await _draftService.GetPolicyContextAsync(id);
            EnsureCanManage(context);

            return Ok(await _draftService.StartAsync(id));
        }

        /// <summary>
        /// Хід. stepIndex у шляху — не прикраса: сервер застосує вибір лише
        /// якщо драфт досі чекає саме на цей крок. Інакше капітан, який
        /// натиснув за мить до спрацювання таймера, мовчки перетер би те, що
        /// таймер уже записав.
        /// </summary>
        [HttpPost("drafts/{id}/steps/{stepIndex}")]
        [Authorize]
        public async Task<ActionResult<DraftDto>> ApplyStep(int id, int stepIndex, [FromBody] ApplyStepDto dto)
        {
            var (context, actingTeamId) = await _draftService.GetPolicyContextAsync(id);

            // isAdmin тут навмисно не питаємо: адміністратор не банить за
            // команду — див. Common/DraftPolicy.cs.
            if (!DraftPolicy.CanAct(context, GetUserIdOrThrow(), actingTeamId))
            {
                throw new ForbiddenException("Зараз хід іншої команди");
            }

            return Ok(await _draftService.ApplyStepAsync(id, stepIndex, dto.Value, wasAutoPicked: false));
        }

        [HttpDelete("drafts/{id}")]
        [Authorize]
        public async Task<ActionResult> Cancel(int id)
        {
            var (context, _) = await _draftService.GetPolicyContextAsync(id);
            EnsureCanManage(context);

            await _draftService.CancelAsync(id);
            return NoContent();
        }

        private void EnsureCanManage(DraftPolicy.Context context)
        {
            if (!DraftPolicy.CanManage(context, GetUserIdOrThrow(), IsAdmin, IsOrganizer))
            {
                throw new ForbiddenException(
                    "Керувати драфтом можуть капітани команд, організатор або адміністратор");
            }
        }
    }
}
