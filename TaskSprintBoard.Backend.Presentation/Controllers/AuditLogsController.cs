using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSprintBoard.Backend.BusinessLogic.Services;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly AuditLogService _service;
    public AuditLogsController(AuditLogService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] string? action = null, [FromQuery] string? entity = null)
        => Ok(await _service.SearchAsync(page, pageSize, search, action, entity));

    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters()
    {
        var (actions, entities) = await _service.GetFiltersAsync();
        return Ok(new { actions, entities });
    }
}
