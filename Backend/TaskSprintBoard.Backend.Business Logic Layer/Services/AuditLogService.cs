using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class AuditLogService
{
    private readonly AuditLogRepository _repository;
    public AuditLogService(AuditLogRepository repository) => _repository = repository;

    public async Task<AuditLogPageDto> SearchAsync(int page, int pageSize, string? search, string? action, string? entity)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, count) = await _repository.SearchAsync(page, pageSize, search, action, entity);
        return new AuditLogPageDto
        {
            Page = page, PageSize = pageSize, TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize),
            Items = items.Select(x => new AuditLogDto
            {
                Id = x.Id, Timestamp = x.OccurredAt, ActorUserId = x.ActorUserId,
                Actor = x.ActorName ?? (x.ActorUserId.HasValue ? $"User · {x.ActorUserId}" : "System"),
                Action = x.ActionDefinition.Name, Entity = x.EntityTypeDefinition.Name, EntityId = x.EntityId, Details = x.Details
            }).ToList()
        };
    }

    public Task RecordLoginAsync(int userId, string name) => _repository.RecordLoginAsync(userId, name);
    public Task<(IReadOnlyList<string> Actions, IReadOnlyList<string> Entities)> GetFiltersAsync() => _repository.GetFiltersAsync();
}
