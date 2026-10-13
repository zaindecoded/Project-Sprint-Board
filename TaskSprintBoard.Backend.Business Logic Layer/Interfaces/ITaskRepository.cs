using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.BusinessLogic.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task<TaskItem?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<TaskItem>> SearchAsync(
        string? search,
        string? status,
        string? priority,
        Guid? projectId,
        Guid? sprintId,
        Guid? userId,
        DateTime? dueDate,
        bool? blocked,
        int skip,
        int take);
    Task<int> CountAsync(
        string? search,
        string? status,
        string? priority,
        Guid? projectId,
        Guid? sprintId,
        Guid? userId,
        DateTime? dueDate,
        bool? blocked);
    Task<TaskItem> AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(Guid id);
}
