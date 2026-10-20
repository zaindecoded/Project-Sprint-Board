using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.BusinessLogic.Interfaces;

public interface IAssignmentRepository
{
    Task<IEnumerable<Assignment>> GetByTaskIdAsync(int taskId);
    Task<Assignment> AddAsync(Assignment assignment);
    Task DeleteAsync(int id);
}
