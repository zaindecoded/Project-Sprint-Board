using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.BusinessLogic.Services;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,ProjectManager")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectService _projectService;

    public ProjectsController(ProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = "", [FromQuery] string? status = null, [FromQuery] string sortBy = "newest")
    {
        int? ownerId = User.IsInRole("ProjectManager") ? User.GetActorUserId() : null;
        var (items, totalCount) = await _projectService.SearchProjectsAsync(search, status, sortBy, page, pageSize, ownerId);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var dtos = items.Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            StatusId = p.StatusId,
            CreatedAt = p.CreatedAt,
            IsActive = p.IsActive
        });

        return Ok(new { items = dtos, page, pageSize, totalCount, totalPages });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        int? ownerId = User.IsInRole("ProjectManager") ? User.GetActorUserId() : null;
        var project = await _projectService.GetByIdAsync(id, ownerId);
        if (project == null) return NotFound();

        var dto = new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status,
            StatusId = project.StatusId,
            CreatedAt = project.CreatedAt,
            IsActive = project.IsActive
        };

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto)
    {
        try
        {
            var project = await _projectService.CreateProjectAsync(dto.Name, dto.Description, dto.StartDate, dto.EndDate, dto.StatusId, User.GetActorUserId());
            var result = new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status,
                StatusId = project.StatusId,
                CreatedAt = project.CreatedAt,
                IsActive = project.IsActive
            };
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Update(int id, [FromBody] ProjectUpdateDto dto)
    {
        try
        {
            var project = await _projectService.UpdateProjectAsync(id, dto.Name, dto.Description, dto.StartDate, dto.EndDate, dto.StatusId, User.GetActorUserId());
            var result = new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status,
                StatusId = project.StatusId,
                CreatedAt = project.CreatedAt,
                IsActive = project.IsActive
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

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _projectService.DeleteProjectAsync(id, User.GetActorUserId());
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
