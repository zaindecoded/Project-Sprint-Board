using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.BusinessLogic.Interfaces;

public interface IStatusHistoryRepository
{
    Task<IEnumerable<StatusHistory>> GetByTaskIdAsync(int taskId);
    Task<StatusHistory> AddAsync(StatusHistory statusHistory);
}
