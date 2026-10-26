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
public class TasksController : ControllerBase
{
    private readonly TaskService _taskService;

    public TasksController(TaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string search = "",
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int? sprintId = null,
        [FromQuery] int? projectId = null,
        [FromQuery] DateTime? dueDate = null,
        [FromQuery] string? dueDatePreset = null,
        [FromQuery] bool? isBlocked = null)
    {
        int? assignedUserId = null;
        if (User.IsInRole("TeamMember"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
                return Forbid();

            assignedUserId = currentUserId;
        }

        var (items, totalCount) = await _taskService.SearchTasksAsync(search, status, priority, projectId, sprintId, assignedUserId, dueDate, dueDatePreset, isBlocked, page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize));

        var responseItems = items.Select(MapToDto);
        return Ok(new { items = responseItems, page, pageSize, totalCount, totalPages });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task == null)
            return NotFound();

        if (User.IsInRole("TeamMember"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId)
                || !task.Assignments.Any(assignment => assignment.UserId == currentUserId && assignment.IsActive && !assignment.IsDeleted))
                return NotFound();
        }

        return Ok(MapToDto(task));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Create([FromBody] TaskCreateDto dto)
    {
        try
        {
            var task = await _taskService.CreateTaskAsync(dto, User.GetActorUserId());
            return StatusCode(201, MapToDto(task));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Update(int id, [FromBody] TaskUpdateDto dto)
    {
        try
        {
            var task = await _taskService.UpdateTaskAsync(id, dto, User.GetActorUserId());
            return Ok(MapToDto(task));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpPatch("{id:int}/complete")]
    [Authorize(Roles = "TeamMember")]
    public async Task<IActionResult> CompleteAssignedTask(int id)
    {
        try
        {
            var task = await _taskService.CompleteAssignedTaskAsync(id, User.GetActorUserId());
            return Ok(MapToDto(task));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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
            await _taskService.DeleteTaskAsync(id, User.GetActorUserId());
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        if (User.IsInRole("TeamMember")
            && (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId)
                || !await _taskService.IsAssignedToUserAsync(id, currentUserId)))
            return NotFound();

        return Ok(new { taskId = id, items = new List<object>() });
    }

    private static TaskDto MapToDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            SprintId = task.SprintId,
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            Status = task.Status,
            EstimateHours = task.EstimateHours,
            ProgressPercentage = task.ProgressPercentage,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            BlockerInformation = task.BlockerInformation,
            CreatedAt = task.CreatedAt,
            IsActive = task.IsActive
        };
    }
}
