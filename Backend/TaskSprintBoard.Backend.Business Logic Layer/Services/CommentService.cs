using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class CommentService
{
    private const int MaxMessageLength = 2000;

    private readonly CommentRepository _commentRepository;
    private readonly TaskRepository _taskRepository;
    private readonly UserRepository _userRepository;

    public CommentService(CommentRepository commentRepository, TaskRepository taskRepository, UserRepository userRepository)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
    }

    public async Task<Comment> CreateCommentAsync(CommentCreateDto dto, int createdBy, int? assignedToUserId = null)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (dto.TaskId == 0)
            throw new InvalidOperationException("TaskId is required.");

        if (dto.UserId <= 0)
            throw new InvalidOperationException("UserId is required.");

        var message = dto.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("Message is required.");

        if (message.Length > MaxMessageLength)
            throw new InvalidOperationException($"Message must be {MaxMessageLength} characters or less.");

        var task = await _taskRepository.GetByIdAsync(dto.TaskId) ?? throw new KeyNotFoundException("Task not found.");
        if (task.IsDeleted)
            throw new KeyNotFoundException("Task not found.");

        if (assignedToUserId.HasValue && !await _taskRepository.IsAssignedToUserAsync(dto.TaskId, assignedToUserId.Value))
            throw new KeyNotFoundException("Task not found.");

        var user = await _userRepository.GetByIdAsync(dto.UserId) ?? throw new KeyNotFoundException("User not found.");
        if (user.IsDeleted)
            throw new KeyNotFoundException("User not found.");

        var comment = new Comment
        {
            TaskId = dto.TaskId,
            UserId = dto.UserId,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsActive = true,
            IsDeleted = false
        };

        return await _commentRepository.AddAsync(comment);
    }

    public async Task<Comment> UpdateCommentAsync(int id, CommentUpdateDto dto, int updatedBy)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var comment = await _commentRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Comment not found.");
        if (comment.IsDeleted)
            throw new KeyNotFoundException("Comment not found.");

        var taskId = dto.TaskId ?? comment.TaskId;
        var userId = dto.UserId ?? comment.UserId;

        if (taskId == 0)
            throw new InvalidOperationException("TaskId is required.");

        if (userId <= 0)
            throw new InvalidOperationException("UserId is required.");

        var task = await _taskRepository.GetByIdAsync(taskId) ?? throw new KeyNotFoundException("Task not found.");
        if (task.IsDeleted)
            throw new KeyNotFoundException("Task not found.");

        var user = await _userRepository.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found.");
        if (user.IsDeleted)
            throw new KeyNotFoundException("User not found.");

        var message = dto.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("Message is required.");

        if (message.Length > MaxMessageLength)
            throw new InvalidOperationException($"Message must be {MaxMessageLength} characters or less.");

        comment.TaskId = taskId;
        comment.UserId = userId;
        comment.Message = message;
        comment.UpdatedAt = DateTime.UtcNow;
        comment.UpdatedBy = updatedBy;

        await _commentRepository.UpdateAsync(comment);
        return comment;
    }

    public async Task DeleteCommentAsync(int id, int deletedBy)
    {
        var comment = await _commentRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Comment not found.");
        if (comment.IsDeleted)
            throw new KeyNotFoundException("Comment not found.");

        comment.IsDeleted = true;
        comment.IsActive = false;
        comment.UpdatedAt = DateTime.UtcNow;
        comment.UpdatedBy = deletedBy;

        await _commentRepository.UpdateAsync(comment);
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        var comment = await _commentRepository.GetByIdWithDetailsAsync(id);
        if (comment != null && comment.IsDeleted)
            return null;

        return comment;
    }

    public Task<bool> IsAssignedToTaskAsync(int taskId, int userId)
    {
        return _taskRepository.IsAssignedToUserAsync(taskId, userId);
    }

    public async Task<(IEnumerable<Comment> Items, int TotalCount)> SearchCommentsAsync(
        string? search,
        int? taskId,
        int? userId,
        int? assignedToUserId,
        int page,
        int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Max(1, pageSize);
        var skip = (safePage - 1) * safePageSize;

        var items = await _commentRepository.SearchAsync(search, taskId, userId, assignedToUserId, skip, safePageSize);
        var totalCount = await _commentRepository.CountAsync(search, taskId, userId, assignedToUserId);

        return (items, totalCount);
    }

    public static CommentResponseDto MapToResponse(Comment comment)
    {
        return new CommentResponseDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserName = comment.User == null ? string.Empty : $"{comment.User.FirstName} {comment.User.LastName}".Trim(),
            Message = comment.Message,
            IsActive = comment.IsActive,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}
