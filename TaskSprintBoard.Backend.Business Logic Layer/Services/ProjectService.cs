using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class ProjectService
{
    private readonly ProjectRepository _projectRepository;

    public ProjectService(ProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    private static readonly string[] AllowedStatuses = ["Planned", "Active", "OnHold", "Completed", "Cancelled"];

    public async Task<Project> CreateProjectAsync(string name, string? description, DateTime? startDate, DateTime? endDate, string status, int createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Project name is required.");

        var today = DateTime.Today;
        if (startDate.HasValue && startDate.Value.Date < today)
            throw new InvalidOperationException("Project start date must be today or a future date.");
        if (endDate.HasValue && endDate.Value.Date < today)
            throw new InvalidOperationException("Project end date must be today or a future date.");
        if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
            throw new InvalidOperationException("Project end date must be on or after its start date.");

        var existing = await _projectRepository.GetByNameAsync(name.Trim());
        if (existing != null)
            throw new InvalidOperationException("Project name already exists.");

        var normalizedStatus = NormalizeStatus(status);
        var entity = new Project
        {
            Name = name.Trim(),
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            Status = normalizedStatus,
            CreatedBy = createdBy,
            IsActive = true,
            IsDeleted = false
        };

        return await _projectRepository.AddAsync(entity);
    }

    public async Task<Project> UpdateProjectAsync(int id, string? name, string? description, DateTime? startDate, DateTime? endDate, string? status, int updatedBy)
    {
        var project = await _projectRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Project not found.");

        if (!string.IsNullOrWhiteSpace(name))
        {
            var duplicate = await _projectRepository.GetByNameAsync(name.Trim());
            if (duplicate != null && duplicate.Id != id)
                throw new InvalidOperationException("Project name already exists.");
            project.Name = name.Trim();
        }

        if (endDate.HasValue && project.EndDate?.Date != endDate.Value.Date
            && await _projectRepository.HasSprintEndingAfterAsync(id, endDate.Value))
            throw new InvalidOperationException("Project end date cannot be earlier than an existing sprint end date.");

        if (description != null) project.Description = description;
        if (startDate.HasValue) project.StartDate = startDate.Value;
        if (endDate.HasValue) project.EndDate = endDate.Value;
        if (status != null) project.Status = NormalizeStatus(status);

        project.UpdatedAt = DateTime.UtcNow;
        project.UpdatedBy = updatedBy;

        if (string.Equals(project.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            await _projectRepository.UpdateAndCompleteWorkAsync(project, updatedBy);
        else
            await _projectRepository.UpdateAsync(project);
        return project;
    }

    private static string NormalizeStatus(string status)
    {
        var normalized = status.Trim();
        var allowed = AllowedStatuses.FirstOrDefault(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));
        return allowed ?? throw new InvalidOperationException($"Project status must be one of: {string.Join(", ", AllowedStatuses)}.");
    }

    public async Task DeleteProjectAsync(int id, int deletedBy)
    {
        var project = await _projectRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Project not found.");
        project.IsDeleted = true;
        project.IsActive = false;
        project.UpdatedAt = DateTime.UtcNow;
        project.UpdatedBy = deletedBy;
        await _projectRepository.UpdateAsync(project);
    }

    // New read/search helpers used by controllers
    public async Task<(IEnumerable<Project> Items, int TotalCount)> SearchProjectsAsync(string? search, int page, int pageSize)
    {
        var skip = Math.Max(0, (page - 1)) * pageSize;
        var items = await _projectRepository.SearchAsync(search, skip, pageSize);

        // Calculate total count (simple approach using GetAllAsync with predicate)
        var allMatching = await _projectRepository.GetAllAsync(x => !x.IsDeleted &&
            (string.IsNullOrWhiteSpace(search) || x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search))));

        var total = allMatching.Count();
        return (items, total);
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        return await _projectRepository.GetByIdAsync(id);
    }
}
