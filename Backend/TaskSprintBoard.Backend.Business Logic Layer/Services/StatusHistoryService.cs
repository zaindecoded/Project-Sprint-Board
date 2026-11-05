using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.BusinessLogic.Validators;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class StatusHistoryService
{
    private readonly StatusHistoryRepository _statusHistoryRepository;
    private readonly TaskRepository _taskRepository;

    public StatusHistoryService(StatusHistoryRepository statusHistoryRepository, TaskRepository taskRepository)
    {
        _statusHistoryRepository = statusHistoryRepository;
        _taskRepository = taskRepository;
    }

    public async Task<StatusHistory?> CreateStatusHistoryAsync(int taskId, int oldStatusId, int newStatusId, string changedBy, string? reason = null)
    {
        if (taskId == 0)
            throw new InvalidOperationException("TaskId is required.");

        var task = await _taskRepository.GetByIdAsync(taskId) ?? throw new KeyNotFoundException("Task not found.");
        if (task.IsDeleted)
            throw new KeyNotFoundException("Task not found.");

        if (oldStatusId <= 0 || newStatusId <= 0)
            throw new InvalidOperationException("OldStatusId and NewStatusId are required.");

        var hasActorId = int.TryParse(changedBy, out var actorId);

        if (oldStatusId == newStatusId)
            return null;

        var statusHistory = new StatusHistory
        {
            TaskId = taskId,
            OldStatusId = oldStatusId,
            NewStatusId = newStatusId,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Reason = reason,
            CreatedBy = hasActorId ? actorId : null,
            IsActive = true,
            IsDeleted = false
        };

        return await _statusHistoryRepository.AddAsync(statusHistory);
    }

    public async Task<StatusHistory?> GetByIdAsync(int id)
    {
        var item = await _statusHistoryRepository.GetByIdWithDetailsAsync(id);
        if (item != null && item.IsDeleted)
            return null;

        return item;
    }

    public async Task<(IEnumerable<StatusHistory> Items, int TotalCount)> SearchAsync(
        int? taskId,
        int? oldStatusId,
        int? newStatusId,
        int page,
        int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Max(1, pageSize);
        var skip = (safePage - 1) * safePageSize;

        var items = await _statusHistoryRepository.SearchAsync(taskId, oldStatusId, newStatusId, skip, safePageSize);
        var totalCount = await _statusHistoryRepository.CountAsync(taskId, oldStatusId, newStatusId);

        return (items, totalCount);
    }

    public async Task<IEnumerable<StatusHistory>> GetByTaskIdAsync(int taskId)
    {
        return await _statusHistoryRepository.GetByTaskIdAsync(taskId);
    }

    public static StatusHistoryResponseDto MapToResponse(StatusHistory item)
    {
        return new StatusHistoryResponseDto
        {
            Id = item.Id,
            TaskId = item.TaskId,
            OldStatusId = item.OldStatusId,
            NewStatusId = item.NewStatusId,
            ChangedAt = item.ChangedAt,
            ChangedBy = item.ChangedBy,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            Reason = item.Reason,
            IsActive = item.IsActive
        };
    }

}
