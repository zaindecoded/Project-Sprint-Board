using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.BusinessLogic.Enums;
using TaskSprintBoard.Backend.BusinessLogic.Services;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,ProjectManager")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveUsers()
    {
        var users = await _userService.GetActiveUsersAsync();
        return Ok(users.Select(user => new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            UserName = user.UserName,
            Role = user.Role
        }));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role) || !Enum.IsDefined(role))
        {
            return BadRequest(new { success = false, message = "Role must be Admin, ProjectManager, or TeamMember.", errors = new[] { "Role must be Admin, ProjectManager, or TeamMember." } });
        }

        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            UserName = dto.UserName.Trim(),
            PasswordHash = dto.Password,
            Role = role.ToString(),
            CreatedBy = User.Identity?.Name ?? "system"
        };

        try
        {
            var createdUser = await _userService.CreateUserAsync(user);
            return StatusCode(201, new UserDto
            {
                Id = createdUser.Id,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                Email = createdUser.Email,
                UserName = createdUser.UserName,
                Role = createdUser.Role
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UserRoleUpdateDto dto)
    {
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role) || !Enum.IsDefined(role))
            return BadRequest(new { success = false, message = "Role must be Admin, ProjectManager, or TeamMember." });

        try
        {
            var user = await _userService.UpdateRoleAsync(id, role.ToString(), User.Identity?.Name ?? "system");
            return Ok(new UserDto { Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, Email = user.Email, UserName = user.UserName, Role = user.Role });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _userService.DeleteUserAsync(id, User.Identity?.Name ?? "system");
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
