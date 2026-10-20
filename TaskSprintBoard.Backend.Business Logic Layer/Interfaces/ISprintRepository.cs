using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.BusinessLogic.Interfaces;

public interface ISprintRepository
{
    Task<Sprint?> GetByIdAsync(int id);
    Task<IEnumerable<Sprint>> GetAllAsync();
    Task<Sprint> AddAsync(Sprint sprint);
    Task UpdateAsync(Sprint sprint);
    Task DeleteAsync(int id);
    Task<IEnumerable<Sprint>> SearchAsync(string? search, int? projectId, int skip, int take);
    Task<int> CountAsync(int? projectId = null, string? search = null);
}
