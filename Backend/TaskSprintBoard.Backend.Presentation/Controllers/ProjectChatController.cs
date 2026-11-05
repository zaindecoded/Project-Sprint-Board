using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

[ApiController]
[Route("api/project-chat")]
[Authorize(Roles = "Admin,ProjectManager")]
public class ProjectChatController : ControllerBase
{
    private const string Help = "I can answer about project descriptions, status and dates; sprint goals, dates and status; and task descriptions, status, priority, progress, estimates, due dates, blockers and assignees.";
    private readonly TaskSprintBoardDbContext _db;

    public ProjectChatController(TaskSprintBoardDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ProjectChatRequest request, CancellationToken cancellationToken)
    {
        var rawQuestion = request.Question?.Trim() ?? string.Empty;
        if (rawQuestion.Length is 0 or > 1000)
            return BadRequest(new { message = "Enter a question of 1 to 1000 characters." });
        var question = Normalize(rawQuestion);
        int? managerId = User.IsInRole("ProjectManager")
            ? User.GetActorUserId()
            : null;

        var projectQuery = _db.Projects.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted);
        if (managerId.HasValue) projectQuery = projectQuery.Where(x => x.CreatedBy == managerId.Value);
        var projects = await projectQuery.ToListAsync(cancellationToken);

        var sprintQuery = _db.Sprints.AsNoTracking()
            .Include(x => x.Project)
            .Where(x => x.IsActive && !x.IsDeleted && x.Project.IsActive && !x.Project.IsDeleted);
        if (managerId.HasValue) sprintQuery = sprintQuery.Where(x => x.Project.CreatedBy == managerId.Value);
        var sprints = await sprintQuery.ToListAsync(cancellationToken);

        var taskQuery = _db.Tasks.AsNoTracking()
            .Include(x => x.PriorityDefinition)
            .Include(x => x.StatusDefinition)
            .Include(x => x.Sprint).ThenInclude(x => x.Project)
            .Include(x => x.Assignments).ThenInclude(x => x.User)
            .Where(x => x.IsActive && !x.IsDeleted && x.Sprint.IsActive && !x.Sprint.IsDeleted && x.Sprint.Project.IsActive && !x.Sprint.Project.IsDeleted);
        if (managerId.HasValue) taskQuery = taskQuery.Where(x => x.Sprint.Project.CreatedBy == managerId.Value);
        var tasks = await taskQuery.ToListAsync(cancellationToken);

        var project = projects.OrderByDescending(x => Normalize(x.Name).Length)
            .FirstOrDefault(x => question.Contains(Normalize(x.Name), StringComparison.Ordinal));
        var sprint = sprints.OrderByDescending(x => Normalize(x.Name).Length)
            .FirstOrDefault(x => question.Contains(Normalize(x.Name), StringComparison.Ordinal));
        var task = tasks.OrderByDescending(x => Normalize(x.Title).Length)
            .FirstOrDefault(x => question.Contains(Normalize(x.Title), StringComparison.Ordinal));
        var projectId = project?.Id ?? sprint?.ProjectId ?? task?.Sprint.ProjectId;
        var projectSprints = sprints.Where(x => projectId == null || x.ProjectId == projectId).ToList();
        var projectTasks = tasks.Where(x => projectId == null || x.Sprint.ProjectId == projectId).ToList();

        var answer = task != null ? AnswerTask(question, task)
            : sprint != null ? AnswerSprint(question, sprint, tasks)
            : project != null ? AnswerProject(question, project, projectSprints, projectTasks)
            : AnswerOverview(question, projects, sprints, tasks);

        return Ok(new { answer });
    }

    private static string AnswerTask(string question, TaskItem task)
    {
        if (Has(question, "assign", "assignee", "owner", "who"))
        {
            var names = task.Assignments.Where(x => x.IsActive && !x.IsDeleted && x.User.IsActive && !x.User.IsDeleted)
                .Select(x => $"{x.User.FirstName} {x.User.LastName}".Trim()).Distinct().ToList();
            return names.Count == 0 ? $"{task.Title} has no assigned team members." : $"{task.Title} is assigned to {string.Join(", ", names)}.";
        }
        if (Has(question, "block", "obstacle"))
            return string.IsNullOrWhiteSpace(task.BlockerInformation) ? $"{task.Title} has no blocker recorded." : $"Blocker for {task.Title}: {task.BlockerInformation}";
        if (Has(question, "progress", "percent", "how much")) return $"{task.Title} is {task.ProgressPercentage}% complete.";
        if (Has(question, "priority", "urgent", "important")) return $"{task.Title} has {task.Priority} priority.";
        if (Has(question, "estimate", "hours", "effort")) return $"{task.Title} is estimated at {task.EstimateHours:0.##} hours.";
        if (Has(question, "due", "deadline", "late", "overdue")) return $"{task.Title} is due {Date(task.DueDate)}.";
        if (Has(question, "start", "started")) return $"{task.Title} starts {Date(task.StartDate)}.";
        if (Has(question, "description", "detail")) return $"{task.Title}: {task.Description}";
        var assignees = task.Assignments.Where(x => x.IsActive && !x.IsDeleted && x.User.IsActive && !x.User.IsDeleted)
            .Select(x => $"{x.User.FirstName} {x.User.LastName}".Trim()).Distinct();
        return $"{task.Title}\nStatus: {task.Status} · Priority: {task.Priority}\nProgress: {task.ProgressPercentage}% · Estimate: {task.EstimateHours:0.##} hours\nDue: {Date(task.DueDate)} · Assignees: {string.Join(", ", assignees.DefaultIfEmpty("none"))}\nBlocker: {task.BlockerInformation ?? "None recorded."}";
    }

    private static string AnswerSprint(string question, Sprint sprint, List<TaskItem> tasks)
    {
        var sprintTasks = tasks.Where(x => x.SprintId == sprint.Id).ToList();
        if (Has(question, "goal", "purpose")) return string.IsNullOrWhiteSpace(sprint.Goal) ? $"{sprint.Name} has no goal recorded." : $"{sprint.Name}\nGoal: {sprint.Goal}";
        if (Has(question, "task", "work item")) return ListTasks(FilterTasks(question, sprintTasks));
        if (Has(question, "start", "end", "date", "deadline", "when"))
            return $"{sprint.Name} runs {Date(sprint.StartDate)} to {Date(sprint.EndDate)}.";
        if (Has(question, "status", "progress", "active", "complete"))
            return $"{sprint.Name}\nStatus: {sprint.Status}\nTasks: {sprintTasks.Count} total · {sprintTasks.Count(x => IsDone(x.Status))} complete.";
        return $"{sprint.Name} · {sprint.Project.Name}\nStatus: {sprint.Status}\nDates: {Date(sprint.StartDate)} – {Date(sprint.EndDate)}\nGoal: {(string.IsNullOrWhiteSpace(sprint.Goal) ? "None recorded." : sprint.Goal)}\nTasks: {sprintTasks.Count}.";
    }

    private static string AnswerProject(string question, Project project, List<Sprint> sprints, List<TaskItem> tasks)
    {
        if (Has(question, "count", "how many")) return $"{project.Name}\nSprints: {sprints.Count} · Tasks: {tasks.Count}.";
        if (Has(question, "description", "detail", "about"))
            return string.IsNullOrWhiteSpace(project.Description) ? $"{project.Name} has no description recorded." : $"{project.Name}: {project.Description}";
        if (Has(question, "task", "blocked", "priority", "assignee")) return ListTasks(FilterTasks(question, tasks));
        if (Has(question, "sprint"))
            return sprints.Count == 0 ? $"{project.Name} has no sprints." : string.Join("\n\n", sprints.Take(10).Select(x => $"• {x.Name}\n  {x.Status} · {Date(x.StartDate)} – {Date(x.EndDate)}")) + (sprints.Count > 10 ? $"\n\nShowing 10 of {sprints.Count}." : string.Empty);
        if (Has(question, "near", "soon", "closest", "deadline", "end", "date", "when"))
            return ProjectDeadline(project);
        if (Has(question, "status", "active", "complete", "progress"))
            return $"{project.Name}\nStatus: {project.Status}\nSprints: {sprints.Count} · Tasks: {tasks.Count} · Complete: {tasks.Count(x => IsDone(x.Status))}.";
        return $"{project.Name}\nStatus: {project.Status}\nDates: {Date(project.StartDate)} – {Date(project.EndDate)}\nSprints: {sprints.Count} · Tasks: {tasks.Count}.";
    }

    private static string AnswerOverview(string question, List<Project> projects, List<Sprint> sprints, List<TaskItem> tasks)
    {
        if (!Has(question, "project", "sprint", "task", "work", "status", "active", "overdue", "late", "block", "due", "deadline", "priority", "assignee", "progress", "team", "help", "hello", "hi", "hey", "upcoming", "soon", "today", "tomorrow"))
            return "I can only answer questions about projects, sprints, and tasks. " + Help;
        if (Has(question, "hello", "hi", "hey")) return "Hi! " + Help;
        if (Has(question, "help")) return Help;
        if (Has(question, "project") && Has(question, "near", "soon", "closest", "nearest", "deadline", "end date", "overdue", "late"))
            return ProjectDeadlines(projects, question);
        if (Has(question, "overdue", "late", "past due"))
        {
            var overdue = tasks.Where(x => x.DueDate?.Date < DateTime.Today && !IsDone(x.Status)).ToList();
            return Has(question, "how many", "count") ? $"There are {overdue.Count} overdue open tasks." : ListTasks(overdue, "There are no overdue open tasks.");
        }
        if (Has(question, "block"))
        {
            var blocked = tasks.Where(x => string.Equals(x.Status, "Blocked", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(x.BlockerInformation)).ToList();
            return Has(question, "how many", "count") ? $"There are {blocked.Count} blocked tasks." : ListTasks(blocked, "There are no blocked tasks.");
        }
        if (Has(question, "due", "deadline", "upcoming", "soon", "near", "today", "tomorrow"))
        {
            var due = FilterTasks(question, tasks);
            return Has(question, "how many", "count") ? $"There are {due.Count} matching tasks." : ListTasks(due);
        }
        if (Has(question, "how many", "count"))
        {
            if (Has(question, "task", "work item")) return $"There are {FilterTasks(question, tasks).Count} matching tasks.";
            if (Has(question, "sprint")) return $"There are {sprints.Count} sprints.";
            if (Has(question, "project")) return $"There are {FilterProjects(question, projects).Count()} matching projects.";
            return $"There are {projects.Count} projects, {sprints.Count} sprints, and {tasks.Count} tasks.";
        }
        if (Has(question, "project"))
        {
            if (projects.Count == 0) return "There are no projects.";
            var matchingProjects = FilterProjects(question, projects).ToList();
            if (matchingProjects.Count == 0) return "There are no projects matching that status.";
            var ordered = Has(question, "closest", "soonest", "next", "nearest")
                ? matchingProjects.OrderBy(x => x.EndDate ?? DateTime.MaxValue)
                : matchingProjects.OrderBy(x => x.Name);
            var listedProjects = ordered.Take(10).ToList();
            return string.Join("\n\n", listedProjects.Select(x => Has(question, "description", "about")
                ? $"• {x.Name}\n  {x.Description ?? "No description recorded."}"
                : Has(question, "deadline", "date", "start", "when", "closest", "soonest", "next", "nearest")
                    ? $"• {x.Name} · {x.Status}\n  Starts {Date(x.StartDate)} · deadline {Date(x.EndDate)}"
                    : $"• {x.Name} · {x.Status}")) + (matchingProjects.Count > listedProjects.Count ? $"\n\nShowing {listedProjects.Count} of {matchingProjects.Count}." : string.Empty);
        }
        if (Has(question, "sprint"))
        {
            var matchingSprints = Has(question, "completed", "complete") ? sprints.Where(x => IsStatus(x.Status, "completed"))
                : Has(question, "active", "current") ? sprints.Where(x => IsStatus(x.Status, "active"))
                : sprints;
            var listedSprints = matchingSprints.ToList();
            if (listedSprints.Count == 0) return "There are no matching sprints.";
            var displayedSprints = listedSprints.Take(10).ToList();
            return string.Join("\n\n", displayedSprints.Select(x => Has(question, "goal", "purpose")
                ? $"• {x.Name} · {x.Project.Name}\n  Goal: {(string.IsNullOrWhiteSpace(x.Goal) ? "None recorded." : x.Goal)}"
                : $"• {x.Name} · {x.Project.Name}\n  {x.Status} · {Date(x.StartDate)} – {Date(x.EndDate)}")) + (listedSprints.Count > displayedSprints.Count ? $"\n\nShowing {displayedSprints.Count} of {listedSprints.Count}." : string.Empty);
        }
        if (Has(question, "task", "work item", "blocker", "assignee")) return ListTasks(FilterTasks(question, tasks));
        if (Has(question, "status", "progress", "active", "complete"))
            return $"There are {projects.Count} projects, {sprints.Count} sprints, and {tasks.Count} tasks. {tasks.Count(x => IsDone(x.Status))} tasks are complete and {tasks.Count(x => string.Equals(x.Status, "Blocked", StringComparison.OrdinalIgnoreCase))} are blocked.";
        return Help;
    }

    private static List<TaskItem> FilterTasks(string question, List<TaskItem> tasks)
    {
        if (Has(question, "overdue", "late")) return tasks.Where(x => x.DueDate?.Date < DateTime.Today && !IsDone(x.Status)).ToList();
        if (Has(question, "today")) return tasks.Where(x => x.DueDate?.Date == DateTime.Today).ToList();
        if (Has(question, "tomorrow")) return tasks.Where(x => x.DueDate?.Date == DateTime.Today.AddDays(1)).ToList();
        if (Has(question, "near", "upcoming", "soon")) return tasks.Where(x => x.DueDate?.Date >= DateTime.Today && x.DueDate?.Date <= DateTime.Today.AddDays(7) && !IsDone(x.Status)).ToList();
        if (Has(question, "due", "deadline")) return tasks.Where(x => x.DueDate.HasValue).OrderBy(x => x.DueDate).ToList();
        if (Has(question, "block")) return tasks.Where(x => string.Equals(x.Status, "Blocked", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(x.BlockerInformation)).ToList();
        if (Has(question, "todo", "to do", "not started")) return tasks.Where(x => Has(Normalize(x.Status), "todo", "not started")).ToList();
        if (Has(question, "progress", "in progress", "started")) return tasks.Where(x => Has(Normalize(x.Status), "progress", "started")).ToList();
        if (Has(question, "complete", "completed", "done")) return tasks.Where(x => IsDone(x.Status)).ToList();
        if (Has(question, "high", "critical", "urgent")) return tasks.Where(x => Has(Normalize(x.Priority), "high", "critical")).ToList();
        if (Has(question, "low")) return tasks.Where(x => Has(Normalize(x.Priority), "low")).ToList();
        if (Has(question, "medium")) return tasks.Where(x => Has(Normalize(x.Priority), "medium")).ToList();
        if (Has(question, "assigned", "assignee", "team member")) return tasks.Where(x => x.Assignments.Any(a => a.IsActive && !a.IsDeleted)).ToList();
        return tasks;
    }

    private static string ProjectDeadlines(List<Project> projects, string question)
    {
        var today = DateTime.Today;
        var cutoff = today.AddDays(30);
        var dated = projects.Where(x => IsStatus(x.Status, "active") && x.EndDate.HasValue);
        var matching = Has(question, "overdue", "late")
            ? dated.Where(x => x.EndDate!.Value.Date < today)
            : Has(question, "near", "soon", "upcoming")
                ? dated.Where(x => x.EndDate!.Value.Date >= today && x.EndDate.Value.Date <= cutoff)
                : dated.Where(x => x.EndDate!.Value.Date >= today);
        var ordered = matching.OrderBy(x => x.EndDate);
        var matches = ordered.ToList();
        var results = Has(question, "closest", "nearest") ? matches.Take(5).ToList() : matches.Take(10).ToList();
        if (results.Count == 0)
            return Has(question, "overdue", "late") ? "No active projects are past their end date."
                : Has(question, "near", "soon", "upcoming") ? "No active projects have an end date in the next 30 days."
                : "No active projects have an end date set.";
        return string.Join("\n\n", results.Select(x => $"• {ProjectDeadline(x)}")) + (matches.Count > results.Count ? $"\n\nShowing {results.Count} of {matches.Count}." : string.Empty);
    }

    private static IEnumerable<Project> FilterProjects(string question, List<Project> projects)
    {
        if (Has(question, "completed", "complete")) return projects.Where(x => IsStatus(x.Status, "completed"));
        if (Has(question, "active", "in progress", "inprogress", "ongoing"))
            return projects.Where(x => IsStatus(x.Status, "active") && (!x.EndDate.HasValue || x.EndDate.Value.Date >= DateTime.Today));
        if (Has(question, "planned")) return projects.Where(x => IsStatus(x.Status, "planned"));
        if (Has(question, "on hold", "onhold", "paused")) return projects.Where(x => IsStatus(x.Status, "onhold"));
        if (Has(question, "cancelled", "canceled")) return projects.Where(x => IsStatus(x.Status, "cancelled"));
        return projects;
    }

    private static string ProjectDeadline(Project project)
    {
        if (!project.EndDate.HasValue) return $"{project.Name} has no end date set.";
        var days = (project.EndDate.Value.Date - DateTime.Today).Days;
        var timing = days switch
        {
            < 0 => $"{Math.Abs(days)} days overdue",
            0 => "due today",
            1 => "due tomorrow",
            _ => $"due in {days} days"
        };
        return $"{project.Name} ({project.Status}) — {Date(project.EndDate)} ({timing}).";
    }

    private static string ListTasks(List<TaskItem> tasks, string empty = "No matching tasks found.")
    {
        if (tasks.Count == 0) return empty;
        var listed = tasks.Take(10).Select(x =>
        {
            var assignees = x.Assignments.Where(a => a.IsActive && !a.IsDeleted && a.User.IsActive && !a.User.IsDeleted)
                .Select(a => $"{a.User.FirstName} {a.User.LastName}".Trim()).Distinct();
            return $"• {x.Title} · {x.Sprint.Project.Name} / {x.Sprint.Name}\n  {x.Status} · {x.Priority} priority · {x.ProgressPercentage}% complete\n  Due {Date(x.DueDate)} · Assigned to {string.Join(", ", assignees.DefaultIfEmpty("nobody"))}\n  Blocker: {x.BlockerInformation ?? "None."}";
        }).ToList();
        return $"Tasks ({tasks.Count}):\n{string.Join("\n\n", listed)}" + (tasks.Count > listed.Count ? $"\n\nShowing {listed.Count} of {tasks.Count}." : string.Empty);
    }

    private static bool IsDone(string status) => new[] { "done", "complete", "completed", "closed" }.Contains(Normalize(status));
    private static bool IsStatus(string status, string expected) => string.Equals(Normalize(status), expected, StringComparison.Ordinal);
    private static bool Has(string value, params string[] terms) => terms.Any(term => value.Contains(Normalize(term), StringComparison.Ordinal));
    private static string Normalize(string value) => Regex.Replace(value.ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim();
    private static string Date(DateTime? value) => value?.ToString("MMM d, yyyy") ?? "no date set";
    private static string Date(DateTime value) => value.ToString("MMM d, yyyy");
}

public sealed class ProjectChatRequest
{
    public string? Question { get; set; }
}
