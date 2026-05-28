using Computational_Practice.DTOs;
using Computational_Practice.Services.Interfaces;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;
using Computational_Practice.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Computational_Practice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("paged")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResponse<UserDto>>> GetPagedUsers([FromQuery] UserFilter filter)
        {
            var users = await _userService.GetPagedAsync(filter, filter);
            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createDto)
        {
            var user = await _userService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateDto)
        {
            var user = await _userService.UpdateAsync(id, updateDto);
            return Ok(user);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteAsync(id);
            if (!result)
            {
                throw new EntityNotFoundException("User", id);
            }

            return NoContent();
        }

        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ActivateUser(int id)
        {
            var result = await _userService.ActivateAsync(id);
            if (!result)
            {
                throw new EntityNotFoundException("User", id);
            }

            return Ok("Користувача успішно активовано");
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeactivateUser(int id)
        {
            var result = await _userService.DeactivateAsync(id);
            if (!result)
            {
                throw new EntityNotFoundException("User", id);
            }

            return Ok("Користувача успішно деактивовано");
        }
    }
}
