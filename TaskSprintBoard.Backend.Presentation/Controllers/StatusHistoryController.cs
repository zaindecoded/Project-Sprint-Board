using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskSprintBoard.Backend.BusinessLogic.Services;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/status-history")]
[Authorize(Roles = "Admin,ProjectManager,TeamMember")]
public class StatusHistoryController : ControllerBase
{
    private readonly StatusHistoryService _statusHistoryService;
    private readonly TaskService _taskService;

    public StatusHistoryController(StatusHistoryService statusHistoryService, TaskService taskService)
    {
        _statusHistoryService = statusHistoryService;
        _taskService = taskService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? taskId = null,
        [FromQuery] string? oldStatus = null,
        [FromQuery] string? newStatus = null)
    {
        var (items, totalCount) = await _statusHistoryService.SearchAsync(taskId, oldStatus, newStatus, page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize));

        var responseItems = items.Select(StatusHistoryService.MapToResponse);
        return Ok(new { items = responseItems, page, pageSize, totalCount, totalPages });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _statusHistoryService.GetByIdAsync(id);
        if (item == null)
            return NotFound();

        return Ok(StatusHistoryService.MapToResponse(item));
    }

    [HttpGet("/api/tasks/{taskId:guid}/status-history")]
    public async Task<IActionResult> GetByTaskId(Guid taskId)
    {
        if (User.IsInRole("TeamMember"))
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId)
                || !await _taskService.IsAssignedToUserAsync(taskId, currentUserId))
                return NotFound();
        }

        var items = await _statusHistoryService.GetByTaskIdAsync(taskId);
        return Ok(items.Select(StatusHistoryService.MapToResponse));
    }
}
