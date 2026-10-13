using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.BusinessLogic.Interfaces;

public interface IAssignmentRepository
{
    Task<IEnumerable<Assignment>> GetByTaskIdAsync(Guid taskId);
    Task<Assignment> AddAsync(Assignment assignment);
    Task DeleteAsync(Guid id);
}
