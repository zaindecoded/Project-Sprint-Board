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
public class CommentsController : ControllerBase
{
    private readonly CommentService _commentService;

    public CommentsController(CommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string search = "",
        [FromQuery] int? taskId = null,
        [FromQuery] int? userId = null)
    {
        int? assignedToUserId = null;
        if (User.IsInRole("TeamMember"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
                return Forbid();

            assignedToUserId = currentUserId;
        }

        var (items, totalCount) = await _commentService.SearchCommentsAsync(search, taskId, userId, assignedToUserId, page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize));

        var responseItems = items.Select(MapToDto);
        return Ok(new { items = responseItems, page, pageSize, totalCount, totalPages });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null)
            return NotFound();

        if (User.IsInRole("TeamMember"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId)
                || !await _commentService.IsAssignedToTaskAsync(comment.TaskId, currentUserId))
                return NotFound();
        }

        return Ok(MapToDto(comment));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CommentCreateDto dto)
    {
        try
        {
            int? assignedToUserId = null;
            if (User.IsInRole("TeamMember"))
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
                    return Forbid();

                dto.UserId = currentUserId;
                assignedToUserId = currentUserId;
            }

            var comment = await _commentService.CreateCommentAsync(dto, User.GetActorUserId(), assignedToUserId);
            return StatusCode(201, MapToDto(comment));
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
    public async Task<IActionResult> Update(int id, [FromBody] CommentUpdateDto dto)
    {
        try
        {
            var comment = await _commentService.UpdateCommentAsync(id, dto, User.GetActorUserId());
            return Ok(MapToDto(comment));
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
            await _commentService.DeleteCommentAsync(id, User.GetActorUserId());
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

    private static CommentResponseDto MapToDto(Comment item)
    {
        return new CommentResponseDto
        {
            Id = item.Id,
            TaskId = item.TaskId,
            UserId = item.UserId,
            UserName = item.User == null ? string.Empty : $"{item.User.FirstName} {item.User.LastName}".Trim(),
            Message = item.Message,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
