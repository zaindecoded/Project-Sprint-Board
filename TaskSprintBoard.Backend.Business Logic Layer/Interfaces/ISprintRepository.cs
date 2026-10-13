using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.BusinessLogic.Interfaces;

public interface ISprintRepository
{
    Task<Sprint?> GetByIdAsync(Guid id);
    Task<IEnumerable<Sprint>> GetAllAsync();
    Task<Sprint> AddAsync(Sprint sprint);
    Task UpdateAsync(Sprint sprint);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Sprint>> SearchAsync(string? search, Guid? projectId, int skip, int take);
    Task<int> CountAsync(Guid? projectId = null, string? search = null);
}
