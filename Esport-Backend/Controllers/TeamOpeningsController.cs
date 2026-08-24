using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.Common.Filters;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    [ApiController]
    [Route("api/team-openings")]
    public class TeamOpeningsController : ApiControllerBase
    {
        private readonly ITeamOpeningService _service;

        public TeamOpeningsController(ITeamOpeningService service) => _service = service;

        /// <summary>Читання відкрите, як і решта списків у проєкті.</summary>
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] TeamOpeningFilter filter) =>
            Ok(await _service.GetPagedAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _service.GetByIdAsync(id));

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateTeamOpeningDto dto) =>
            Ok(await _service.CreateAsync(dto, GetUserIdOrThrow(), IsAdmin));

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTeamOpeningDto dto) =>
            Ok(await _service.UpdateAsync(id, dto, GetUserIdOrThrow(), IsAdmin));

        [HttpPost("{id:int}/close")]
        [Authorize]
        public async Task<IActionResult> Close(int id) =>
            Ok(await _service.CloseAsync(id, GetUserIdOrThrow(), IsAdmin));
    }
}
