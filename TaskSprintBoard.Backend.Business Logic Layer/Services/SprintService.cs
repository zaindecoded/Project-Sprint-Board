using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class SprintService
{
    private readonly SprintRepository _sprintRepository;
    private readonly ProjectRepository _projectRepository;

    public SprintService(SprintRepository sprintRepository, ProjectRepository projectRepository)
    {
        _sprintRepository = sprintRepository;
        _projectRepository = projectRepository;
    }

    public async Task<Sprint> CreateSprintAsync(SprintCreateDto dto, int createdBy)
    {
        if (dto.ProjectId == 0)
            throw new InvalidOperationException("Project is required.");

        var project = await _projectRepository.GetByIdAsync(dto.ProjectId)
            ?? throw new KeyNotFoundException("Project not found.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Sprint name is required.");

        var today = DateTime.Today;
        if (dto.StartDate.Date < today)
            throw new InvalidOperationException("Sprint start date must be today or a future date.");
        if (dto.EndDate.Date < today)
            throw new InvalidOperationException("Sprint end date must be today or a future date.");

        if (dto.EndDate < dto.StartDate)
            throw new InvalidOperationException("Sprint end date must be on or after its start date.");
        if (project.EndDate.HasValue && dto.EndDate.Date > project.EndDate.Value.Date)
            throw new InvalidOperationException("Sprint end date cannot be later than the project end date.");

        var trimmedName = dto.Name.Trim();
        if (await _sprintRepository.ExistsByNameForProjectAsync(dto.ProjectId, trimmedName))
            throw new InvalidOperationException("Sprint name already exists for this project.");

        var entity = new Sprint
        {
            ProjectId = project.Id,
            Name = trimmedName,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Goal = dto.Goal.Trim(),
            Status = dto.Status,
            CreatedBy = createdBy,
            IsActive = true,
            IsDeleted = false
        };

        return await _sprintRepository.AddAsync(entity);
    }

    public async Task<Sprint> UpdateSprintAsync(int id, SprintUpdateDto dto, int updatedBy)
    {
        var sprint = await _sprintRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Sprint not found.");
        var originalProjectId = sprint.ProjectId;
        var originalEndDate = sprint.EndDate.Date;

        if (dto.ProjectId.HasValue)
        {
            if (dto.ProjectId.Value == 0)
                throw new InvalidOperationException("Project is required.");

            var project = await _projectRepository.GetByIdAsync(dto.ProjectId.Value)
                ?? throw new KeyNotFoundException("Project not found.");
            sprint.ProjectId = project.Id;
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var name = dto.Name.Trim();
            if (await _sprintRepository.ExistsByNameForProjectAsync(sprint.ProjectId, name, sprint.Id))
                throw new InvalidOperationException("Sprint name already exists for this project.");
            sprint.Name = name;
        }

        if (dto.StartDate.HasValue) sprint.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) sprint.EndDate = dto.EndDate.Value;
        if (dto.Goal != null) sprint.Goal = dto.Goal.Trim();
        if (dto.Status != null) sprint.Status = dto.Status;

        if (sprint.EndDate < sprint.StartDate)
            throw new InvalidOperationException("Sprint end date must be on or after its start date.");

        if (sprint.ProjectId != originalProjectId || sprint.EndDate.Date != originalEndDate)
        {
            var projectForSprint = await _projectRepository.GetByIdAsync(sprint.ProjectId)
                ?? throw new KeyNotFoundException("Project not found.");
            if (projectForSprint.EndDate.HasValue && sprint.EndDate.Date > projectForSprint.EndDate.Value.Date)
                throw new InvalidOperationException("Sprint end date cannot be later than the project end date.");
            if (await _sprintRepository.HasTaskDueAfterAsync(sprint.Id, sprint.EndDate))
                throw new InvalidOperationException("Sprint end date cannot be earlier than an existing task due date.");
        }

        sprint.UpdatedAt = DateTime.UtcNow;
        sprint.UpdatedBy = updatedBy;

        await _sprintRepository.UpdateAsync(sprint);
        return sprint;
    }

    public async Task DeleteSprintAsync(int id, int deletedBy)
    {
        var sprint = await _sprintRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Sprint not found.");
        sprint.IsDeleted = true;
        sprint.IsActive = false;
        sprint.UpdatedAt = DateTime.UtcNow;
        sprint.UpdatedBy = deletedBy;
        await _sprintRepository.UpdateAsync(sprint);
    }

    public async Task<Sprint?> GetByIdAsync(int id)
    {
        return await _sprintRepository.GetByIdAsync(id);
    }

    public async Task<(IEnumerable<Sprint> Items, int TotalCount)> SearchSprintsAsync(string? search, int? projectId, string? status, int page, int pageSize)
    {
        var skip = Math.Max(0, page - 1) * pageSize;
        var items = await _sprintRepository.SearchAsync(search, projectId, status, skip, pageSize);
        var count = await _sprintRepository.CountSearchAsync(search, projectId, status);
        return (items, count);
    }
}
