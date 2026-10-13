using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.BusinessLogic.Services;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,ProjectManager")]
public class SprintsController : ControllerBase
{
    private readonly SprintService _sprintService;

    public SprintsController(SprintService sprintService)
    {
        _sprintService = sprintService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = "")
    {
        var (items, totalCount) = await _sprintService.SearchSprintsAsync(search, projectId, page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize));

        var dtoItems = items.Select(s => new SprintDto
        {
            Id = s.Id,
            ProjectId = s.ProjectId,
            Name = s.Name,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Goal = s.Goal,
            Status = s.Status,
            CreatedAt = s.CreatedAt,
            IsActive = s.IsActive
        });

        return Ok(new { items = dtoItems, page, pageSize, totalCount, totalPages });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sprint = await _sprintService.GetByIdAsync(id);
        if (sprint == null)
            return NotFound();

        var dto = new SprintDto
        {
            Id = sprint.Id,
            ProjectId = sprint.ProjectId,
            Name = sprint.Name,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            Goal = sprint.Goal,
            Status = sprint.Status,
            CreatedAt = sprint.CreatedAt,
            IsActive = sprint.IsActive
        };

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Create([FromBody] SprintCreateDto dto)
    {
        try
        {
            var sprint = await _sprintService.CreateSprintAsync(dto, User.Identity?.Name ?? "system");
            var result = new SprintDto
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                Name = sprint.Name,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Goal = sprint.Goal,
                Status = sprint.Status,
                CreatedAt = sprint.CreatedAt,
                IsActive = sprint.IsActive
            };
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SprintUpdateDto dto)
    {
        try
        {
            var sprint = await _sprintService.UpdateSprintAsync(id, dto, User.Identity?.Name ?? "system");
            var result = new SprintDto
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                Name = sprint.Name,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Goal = sprint.Goal,
                Status = sprint.Status,
                CreatedAt = sprint.CreatedAt,
                IsActive = sprint.IsActive
            };
            return Ok(result);
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

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _sprintService.DeleteSprintAsync(id, User.Identity?.Name ?? "system");
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
}
