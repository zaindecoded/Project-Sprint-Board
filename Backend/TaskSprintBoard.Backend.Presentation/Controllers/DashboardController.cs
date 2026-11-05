using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSprintBoard.Backend.BusinessLogic.Services;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,ProjectManager,TeamMember")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = User.IsInRole("TeamMember") ? User.GetActorUserId() : (int?)null;
        int? managerId = User.IsInRole("ProjectManager") ? User.GetActorUserId() : null;
        var summary = await _dashboardService.GetSummaryAsync(userId, managerId);
        return Ok(summary);
    }

    [HttpGet("attention-tasks")]
    public async Task<IActionResult> GetAttentionTasks()
    {
        var userId = User.IsInRole("TeamMember") ? User.GetActorUserId() : (int?)null;
        int? managerId = User.IsInRole("ProjectManager") ? User.GetActorUserId() : null;
        var tasks = await _dashboardService.GetAttentionTasksAsync(userId, managerId);
        return Ok(new { items = tasks });
    }
}
