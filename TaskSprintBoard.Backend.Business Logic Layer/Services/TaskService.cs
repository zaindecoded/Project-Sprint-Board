using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.BusinessLogic.Validators;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class TaskService
{
    private readonly TaskRepository _taskRepository;
    private readonly StatusHistoryRepository _historyRepository;
    private readonly SprintRepository _sprintRepository;

    public TaskService(TaskRepository taskRepository, StatusHistoryRepository historyRepository, SprintRepository sprintRepository)
    {
        _taskRepository = taskRepository;
        _historyRepository = historyRepository;
        _sprintRepository = sprintRepository;
    }

    public async Task<TaskItem> CreateTaskAsync(TaskCreateDto dto, string createdBy)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (dto.SprintId == Guid.Empty)
            throw new InvalidOperationException("SprintId is required.");

        var sprint = await _sprintRepository.GetByIdWithProjectAsync(dto.SprintId)
            ?? throw new KeyNotFoundException("Sprint not found.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidOperationException("Task title is required.");

        var title = dto.Title.Trim();
        if (await _taskRepository.ExistsByTitleInSprintAsync(dto.SprintId, title))
            throw new InvalidOperationException("Task title already exists for this sprint.");

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var normalizedStatus = dto.Status.Trim();
            TaskValidator.ValidateStatus(normalizedStatus);
            dto.Status = Enum.Parse<BusinessLogic.Enums.TaskStatus>(normalizedStatus, true).ToString();
        }

        if (dto.EstimateHours <= 0)
            throw new InvalidOperationException("Estimate must be greater than zero.");

        if (dto.ProgressPercentage < 0 || dto.ProgressPercentage > 100)
            throw new InvalidOperationException("Progress must be between 0 and 100.");

        var today = DateTime.Today;
        if (dto.StartDate.HasValue && dto.StartDate.Value.Date < today)
            throw new InvalidOperationException("Task start date must be today or a future date.");
        if (dto.DueDate.HasValue && dto.DueDate.Value.Date < today)
            throw new InvalidOperationException("Task due date must be today or a future date.");

        if (dto.StartDate.HasValue && dto.DueDate.HasValue && dto.DueDate.Value < dto.StartDate.Value)
            throw new InvalidOperationException("Due date cannot be earlier than the start date.");
        if (dto.DueDate.HasValue && dto.DueDate.Value.Date > sprint.EndDate.Date)
            throw new InvalidOperationException("Task due date cannot be later than its sprint end date.");

        var task = new TaskItem
        {
            SprintId = dto.SprintId,
            Title = title,
            Description = dto.Description.Trim(),
            Priority = dto.Priority,
            Status = dto.Status.Trim(),
            EstimateHours = dto.EstimateHours,
            ProgressPercentage = dto.ProgressPercentage,
            StartDate = dto.StartDate,
            DueDate = dto.DueDate,
            BlockerInformation = dto.BlockerInformation,
            CreatedBy = createdBy,
            IsActive = true,
            IsDeleted = false
        };

        await _taskRepository.AddAsync(task);
        return task;
    }

    public async Task<TaskItem> UpdateTaskAsync(Guid id, TaskUpdateDto dto, string updatedBy)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var task = await _taskRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Task not found.");
        if (task.IsDeleted)
            throw new KeyNotFoundException("Task not found.");

        if (dto.SprintId.HasValue && dto.SprintId.Value == Guid.Empty)
            throw new InvalidOperationException("SprintId is required.");

        if (dto.SprintId.HasValue && !await _taskRepository.SprintExistsAsync(dto.SprintId.Value))
            throw new KeyNotFoundException("Sprint not found.");

        if (dto.EstimateHours.HasValue && dto.EstimateHours.Value <= 0)
            throw new InvalidOperationException("Estimate must be greater than zero.");

        if (dto.ProgressPercentage.HasValue && (dto.ProgressPercentage.Value < 0 || dto.ProgressPercentage.Value > 100))
            throw new InvalidOperationException("Progress must be between 0 and 100.");

        if (dto.StartDate.HasValue && dto.DueDate.HasValue && dto.DueDate.Value < dto.StartDate.Value)
            throw new InvalidOperationException("Due date cannot be earlier than the start date.");

        var updatedSprintId = dto.SprintId ?? task.SprintId;
        var updatedDueDate = dto.DueDate ?? task.DueDate;
        var sprintChanged = updatedSprintId != task.SprintId;
        var dueDateChanged = dto.DueDate.HasValue && dto.DueDate.Value.Date != task.DueDate?.Date;
        if (sprintChanged || dueDateChanged)
        {
            var sprint = await _sprintRepository.GetByIdWithProjectAsync(updatedSprintId)
                ?? throw new KeyNotFoundException("Sprint not found.");
            if (updatedDueDate.HasValue && updatedDueDate.Value.Date > sprint.EndDate.Date)
                throw new InvalidOperationException("Task due date cannot be later than its sprint end date.");
            if (updatedDueDate.HasValue && await _taskRepository.HasAssignmentAfterAsync(task.Id, updatedDueDate.Value))
                throw new InvalidOperationException("Task due date cannot be earlier than an existing assignment date.");
        }

        var oldStatus = task.Status;
        var newStatus = dto.Status?.Trim() ?? oldStatus;

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            TaskValidator.ValidateStatus(newStatus);
            newStatus = Enum.Parse<BusinessLogic.Enums.TaskStatus>(newStatus, true).ToString();
        }

        if (!string.IsNullOrWhiteSpace(dto.Status) && !IsValidTransition(oldStatus, newStatus))
            throw new InvalidOperationException($"Invalid status transition from {oldStatus} to {newStatus}.");

        if (dto.SprintId.HasValue && dto.SprintId.Value != task.SprintId)
        {
            var title = string.IsNullOrWhiteSpace(dto.Title) ? task.Title : dto.Title.Trim();
            if (await _taskRepository.ExistsByTitleInSprintAsync(dto.SprintId.Value, title, task.Id))
                throw new InvalidOperationException("Task title already exists for this sprint.");
        }
        else if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            var title = dto.Title.Trim();
            if (await _taskRepository.ExistsByTitleInSprintAsync(task.SprintId, title, task.Id))
                throw new InvalidOperationException("Task title already exists for this sprint.");
        }

        if (dto.Title != null) task.Title = dto.Title.Trim();
        if (dto.Description != null) task.Description = dto.Description.Trim();
        if (dto.Priority != null) task.Priority = dto.Priority;
        if (dto.Status != null) task.Status = newStatus;
        if (dto.EstimateHours.HasValue) task.EstimateHours = dto.EstimateHours.Value;
        if (dto.ProgressPercentage.HasValue) task.ProgressPercentage = dto.ProgressPercentage.Value;
        if (dto.StartDate.HasValue) task.StartDate = dto.StartDate.Value;
        if (dto.DueDate.HasValue) task.DueDate = dto.DueDate.Value;
        if (dto.BlockerInformation != null) task.BlockerInformation = dto.BlockerInformation;
        if (dto.SprintId.HasValue) task.SprintId = dto.SprintId.Value;

        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = updatedBy;

        await _taskRepository.UpdateAsync(task);

        if (!string.IsNullOrWhiteSpace(dto.Status) && !string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
        {
            await _historyRepository.AddAsync(new StatusHistory
            {
                TaskId = task.Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedBy = updatedBy,
                ChangedAt = DateTime.UtcNow,
                Reason = "Status updated via task workflow.",
                CreatedBy = updatedBy,
                IsActive = true,
                IsDeleted = false
            });
        }

        return task;
    }

    public async Task DeleteTaskAsync(Guid id, string deletedBy)
    {
        var task = await _taskRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Task not found.");
        if (task.IsDeleted)
            throw new KeyNotFoundException("Task not found.");

        task.IsDeleted = true;
        task.IsActive = false;
        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = deletedBy;
        await _taskRepository.UpdateAsync(task);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(id);
        if (task != null && task.IsDeleted)
            return null;

        return task;
    }

    public async Task<(IEnumerable<TaskItem> Items, int TotalCount)> SearchTasksAsync(
        string? search,
        string? status,
        string? priority,
        Guid? projectId,
        Guid? sprintId,
        Guid? userId,
        DateTime? dueDate,
        bool? blocked,
        int page,
        int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Max(1, pageSize);
        var skip = (safePage - 1) * safePageSize;

        var items = await _taskRepository.SearchAsync(search, status, priority, projectId, sprintId, userId, dueDate, blocked, skip, safePageSize);
        var totalCount = await _taskRepository.CountAsync(search, status, priority, projectId, sprintId, userId, dueDate, blocked);

        return (items, totalCount);
    }

    public Task<bool> IsAssignedToUserAsync(Guid taskId, Guid userId)
    {
        return _taskRepository.IsAssignedToUserAsync(taskId, userId);
    }

    private static bool IsValidTransition(string oldStatus, string newStatus)
    {
        var validTransitions = new Dictionary<string, string[]>
        {
            ["Todo"] = ["InProgress"],
            ["InProgress"] = ["Blocked", "InReview", "Done"],
            ["Blocked"] = ["InProgress"],
            ["InReview"] = ["InProgress", "Done"],
            ["Done"] = ["InProgress"],
            ["Cancelled"] = ["InProgress"]
        };

        var normalizedOldStatus = oldStatus?.Trim();
        var normalizedNewStatus = newStatus?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedOldStatus) || string.IsNullOrWhiteSpace(normalizedNewStatus))
            return false;

        if (!validTransitions.TryGetValue(normalizedOldStatus, out var allowed))
        {
            var enumName = Enum.GetName(typeof(TaskSprintBoard.Backend.BusinessLogic.Enums.TaskStatus), normalizedOldStatus);
            if (enumName == null)
                return false;

            normalizedOldStatus = enumName;
            if (!validTransitions.TryGetValue(normalizedOldStatus, out allowed))
                return false;
        }

        return allowed.Any(item => string.Equals(item, normalizedNewStatus, StringComparison.OrdinalIgnoreCase));
    }
}
