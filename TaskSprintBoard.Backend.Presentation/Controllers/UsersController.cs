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
    public async Task<IActionResult> GetActiveUsers([FromQuery] bool includeInactive = false)
    {
        if (includeInactive && !User.IsInRole("Admin")) return Forbid();
        var users = includeInactive
            ? await _userService.GetAllUsersAsync()
            : await _userService.GetActiveUsersAsync();
        return Ok(users.Select(user => new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            UserName = user.UserName,
            Role = user.Role,
            RoleId = user.RoleId,
            IsActive = user.IsActive && !user.IsDeleted
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
            CreatedBy = User.GetActorUserId()
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
                Role = createdUser.Role,
                RoleId = createdUser.RoleId,
                IsActive = createdUser.IsActive && !createdUser.IsDeleted
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpPatch("{id:int}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UserRoleUpdateDto dto)
    {
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role) || !Enum.IsDefined(role))
            return BadRequest(new { success = false, message = "Role must be Admin, ProjectManager, or TeamMember." });

        try
        {
            var user = await _userService.UpdateRoleAsync(id, role.ToString(), User.GetActorUserId());
            return Ok(new UserDto { Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, Email = user.Email, UserName = user.UserName, Role = user.Role, RoleId = user.RoleId, IsActive = user.IsActive && !user.IsDeleted });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
    {
        string? role = null;
        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            if (!Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole) || !Enum.IsDefined(parsedRole))
                return BadRequest(new { success = false, message = "Role must be Admin, ProjectManager, or TeamMember." });
            role = parsedRole.ToString();
        }
        if ((dto.FirstName is not null && string.IsNullOrWhiteSpace(dto.FirstName)) || (dto.LastName is not null && string.IsNullOrWhiteSpace(dto.LastName)) || (dto.Email is not null && string.IsNullOrWhiteSpace(dto.Email)) || (dto.UserName is not null && string.IsNullOrWhiteSpace(dto.UserName)))
            return BadRequest(new { success = false, message = "Provided user fields cannot be blank." });
        try
        {
            var user = await _userService.UpdateUserAsync(id, dto.FirstName?.Trim(), dto.LastName?.Trim(), dto.Email?.Trim(), dto.UserName?.Trim(), role, dto.Password, User.GetActorUserId());
            return Ok(new UserDto { Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, Email = user.Email, UserName = user.UserName, Role = user.Role, RoleId = user.RoleId, IsActive = user.IsActive && !user.IsDeleted });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } }); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _userService.DeleteUserAsync(id, User.GetActorUserId());
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(int id)
    {
        try
        {
            var user = await _userService.ActivateUserAsync(id, User.GetActorUserId());
            return Ok(new UserDto { Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, Email = user.Email, UserName = user.UserName, Role = user.Role, RoleId = user.RoleId, IsActive = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
