using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.BusinessLogic.Services;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AuditLogService _auditLogService;

    public AuthController(AuthService authService, AuditLogService auditLogService)
    {
        _authService = authService;
        _auditLogService = auditLogService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Email and password are required." });

        try
        {
            var loginResult = await _authService.LoginAsync(request.Email, request.Password);
            await _auditLogService.RecordLoginAsync(loginResult.UserId, loginResult.Name);
            return Ok(new
            {
                success = true,
                token = loginResult.Token,
                userId = loginResult.UserId,
                name = loginResult.Name,
                email = loginResult.Email,
                role = loginResult.Role,
                roleId = loginResult.RoleId,
                expiresAt = loginResult.ExpiresAt
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }
}
