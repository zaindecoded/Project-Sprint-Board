using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.BusinessLogic.Interfaces;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByTaskIdAsync(int taskId);
    Task<Comment> AddAsync(Comment comment);
    Task UpdateAsync(Comment comment);
    Task DeleteAsync(int id);
}
