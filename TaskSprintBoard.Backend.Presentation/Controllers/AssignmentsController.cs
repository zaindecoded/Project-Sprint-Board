using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.BusinessLogic.Services;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,ProjectManager,TeamMember")]
public class AssignmentsController : ControllerBase
{
    private readonly AssignmentService _assignmentService;

    public AssignmentsController(AssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string search = "",
        [FromQuery] int? taskId = null,
        [FromQuery] int? userId = null,
        [FromQuery] bool? isActive = null)
    {
        if (User.IsInRole("TeamMember"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
                return Forbid();

            userId = currentUserId;
            isActive = true;
        }

        var (items, totalCount) = await _assignmentService.SearchAssignmentsAsync(search, taskId, userId, isActive, page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize));

        var responseItems = items.Select(MapToDto);
        return Ok(new { items = responseItems, page, pageSize, totalCount, totalPages });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var assignment = await _assignmentService.GetByIdAsync(id);
        if (assignment == null)
            return NotFound();

        if (User.IsInRole("TeamMember")
            && (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId)
                || assignment.UserId != currentUserId
                || !assignment.IsActive))
            return NotFound();

        return Ok(MapToDto(assignment));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Create([FromBody] AssignmentCreateDto dto)
    {
        try
        {
            var assignment = await _assignmentService.CreateAssignmentAsync(dto, User.GetActorUserId());
            return StatusCode(201, MapToDto(assignment));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Update(int id, [FromBody] AssignmentUpdateDto dto)
    {
        try
        {
            var assignment = await _assignmentService.UpdateAssignmentAsync(id, dto, User.GetActorUserId());
            return Ok(MapToDto(assignment));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _assignmentService.DeleteAssignmentAsync(id, User.GetActorUserId());
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    private static AssignmentResponseDto MapToDto(Assignment item)
    {
        return new AssignmentResponseDto
        {
            Id = item.Id,
            TaskId = item.TaskId,
            UserId = item.UserId,
            UserName = item.User == null ? string.Empty : $"{item.User.FirstName} {item.User.LastName}".Trim(),
            AssignedAt = item.AssignedAt,
            UnassignedAt = item.UnassignedAt,
            RoleInTask = item.RoleInTask,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
