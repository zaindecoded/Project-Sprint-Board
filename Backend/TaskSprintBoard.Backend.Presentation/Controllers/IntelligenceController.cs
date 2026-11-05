using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.BusinessLogic.Services;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntelligenceController : ControllerBase
{
    private readonly AiRiskService _aiRiskService;

    public IntelligenceController(AiRiskService aiRiskService)
    {
        _aiRiskService = aiRiskService;
    }

    [HttpPost("evaluate")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Evaluate([FromBody] AiRiskEvaluationInputDto dto)
    {
        try
        {
            var result = await _aiRiskService.EvaluateAsync(dto, User.GetActorUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpGet("tasks/{taskId:int}/latest")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> GetLatest(int taskId)
    {
        var result = await _aiRiskService.GetLatestAsync(taskId);
        return result == null ? NoContent() : Ok(result);
    }
}
