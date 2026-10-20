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
        var summary = await _dashboardService.GetSummaryAsync(userId);
        return Ok(summary);
    }

    [HttpGet("attention-tasks")]
    public async Task<IActionResult> GetAttentionTasks()
    {
        var userId = User.IsInRole("TeamMember") ? User.GetActorUserId() : (int?)null;
        var tasks = await _dashboardService.GetAttentionTasksAsync(userId);
        return Ok(new { items = tasks });
    }
}
