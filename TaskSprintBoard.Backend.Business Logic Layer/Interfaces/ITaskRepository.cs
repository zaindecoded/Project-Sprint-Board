using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.BusinessLogic.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(int id);
    Task<TaskItem?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<TaskItem>> SearchAsync(
        string? search,
        string? status,
        string? priority,
        int? projectId,
        int? sprintId,
        int? userId,
        DateTime? dueDate,
        bool? blocked,
        int skip,
        int take);
    Task<int> CountAsync(
        string? search,
        string? status,
        string? priority,
        int? projectId,
        int? sprintId,
        int? userId,
        DateTime? dueDate,
        bool? blocked);
    Task<TaskItem> AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(int id);
}
