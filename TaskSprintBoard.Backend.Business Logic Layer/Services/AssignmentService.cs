using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class AssignmentService
{
    private readonly AssignmentRepository _assignmentRepository;
    private readonly TaskRepository _taskRepository;
    private readonly UserRepository _userRepository;

    public AssignmentService(AssignmentRepository assignmentRepository, TaskRepository taskRepository, UserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
    }

    public async Task<Assignment> CreateAssignmentAsync(AssignmentCreateDto dto, int createdBy)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (dto.TaskId == 0)
            throw new InvalidOperationException("TaskId is required.");

        if (dto.UserId <= 0)
            throw new InvalidOperationException("UserId is required.");

        if (dto.AssignedAt.HasValue && dto.AssignedAt.Value.Date < DateTime.Today)
            throw new InvalidOperationException("Assignment date must be today or a future date.");

        var task = await _taskRepository.GetByIdAsync(dto.TaskId) ?? throw new KeyNotFoundException("Task not found.");
        if (task.IsDeleted)
            throw new KeyNotFoundException("Task not found.");

        var assignedAt = dto.AssignedAt ?? DateTime.UtcNow;
        if (task.DueDate.HasValue && assignedAt.Date > task.DueDate.Value.Date)
            throw new InvalidOperationException("Assignment date cannot be later than the task due date.");

        var user = await _userRepository.GetByIdAsync(dto.UserId) ?? throw new KeyNotFoundException("User not found.");
        if (user.IsDeleted)
            throw new KeyNotFoundException("User not found.");

        if (await _assignmentRepository.ExistsActiveAssignmentAsync(dto.TaskId, dto.UserId))
            throw new InvalidOperationException("User already assigned to this task.");

        var assignment = new Assignment
        {
            TaskId = dto.TaskId,
            UserId = dto.UserId,
            AssignedAt = assignedAt,
            UnassignedAt = null,
            RoleInTask = dto.RoleInTask,
            CreatedBy = createdBy,
            IsActive = true,
            IsDeleted = false
        };

        return await _assignmentRepository.AddAsync(assignment);
    }

    public async Task<Assignment> UpdateAssignmentAsync(int id, AssignmentUpdateDto dto, int updatedBy)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var assignment = await _assignmentRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Assignment not found.");
        if (assignment.IsDeleted)
            throw new KeyNotFoundException("Assignment not found.");

        var taskId = dto.TaskId ?? assignment.TaskId;
        var userId = dto.UserId ?? assignment.UserId;

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

        var proposedAssignedAt = dto.AssignedAt ?? assignment.AssignedAt;
        var assignmentRelationChanged = taskId != assignment.TaskId
            || (dto.AssignedAt.HasValue && dto.AssignedAt.Value.Date != assignment.AssignedAt.Date);
        if (assignmentRelationChanged && task.DueDate.HasValue && proposedAssignedAt.Date > task.DueDate.Value.Date)
            throw new InvalidOperationException("Assignment date cannot be later than the task due date.");

        if (dto.TaskId.HasValue || dto.UserId.HasValue)
        {
            if (await _assignmentRepository.ExistsActiveAssignmentAsync(taskId, userId, assignment.Id))
                throw new InvalidOperationException("User already assigned to this task.");
        }

        if (dto.AssignedAt.HasValue)
            assignment.AssignedAt = dto.AssignedAt.Value;

        if (dto.UnassignedAt.HasValue)
            assignment.UnassignedAt = dto.UnassignedAt.Value;

        if (dto.RoleInTask != null)
            assignment.RoleInTask = dto.RoleInTask;

        if (dto.IsActive.HasValue)
        {
            assignment.IsActive = dto.IsActive.Value;
            if (!assignment.IsActive && !assignment.UnassignedAt.HasValue)
                assignment.UnassignedAt = DateTime.UtcNow;
            if (assignment.IsActive)
                assignment.UnassignedAt = null;
        }

        if (dto.TaskId.HasValue)
            assignment.TaskId = dto.TaskId.Value;

        if (dto.UserId.HasValue)
            assignment.UserId = dto.UserId.Value;

        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = updatedBy;

        await _assignmentRepository.UpdateAsync(assignment);
        return assignment;
    }

    public async Task DeleteAssignmentAsync(int id, int deletedBy)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Assignment not found.");
        if (assignment.IsDeleted)
            throw new KeyNotFoundException("Assignment not found.");

        assignment.IsDeleted = true;
        assignment.IsActive = false;
        assignment.UnassignedAt ??= DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = deletedBy;

        await _assignmentRepository.UpdateAsync(assignment);
    }

    public async Task<Assignment?> GetByIdAsync(int id)
    {
        var item = await _assignmentRepository.GetByIdWithDetailsAsync(id);
        if (item != null && item.IsDeleted)
            return null;

        return item;
    }

    public async Task<(IEnumerable<Assignment> Items, int TotalCount)> SearchAssignmentsAsync(
        string? search,
        int? taskId,
        int? userId,
        bool? isActive,
        string? sortBy,
        int page,
        int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Max(1, pageSize);
        var skip = (safePage - 1) * safePageSize;

        var items = await _assignmentRepository.SearchAsync(search, taskId, userId, isActive, sortBy, skip, safePageSize);
        var totalCount = await _assignmentRepository.CountAsync(search, taskId, userId, isActive);

        return (items, totalCount);
    }

    public static AssignmentResponseDto MapToResponse(Assignment assignment)
    {
        return new AssignmentResponseDto
        {
            Id = assignment.Id,
            TaskId = assignment.TaskId,
            UserId = assignment.UserId,
            UserName = assignment.User == null ? string.Empty : $"{assignment.User.FirstName} {assignment.User.LastName}".Trim(),
            AssignedAt = assignment.AssignedAt,
            UnassignedAt = assignment.UnassignedAt,
            RoleInTask = assignment.RoleInTask,
            IsActive = assignment.IsActive,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt
        };
    }
}
