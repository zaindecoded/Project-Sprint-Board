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

        var projects = await _db.Projects.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted).ToListAsync(cancellationToken);
        var sprints = await _db.Sprints.AsNoTracking()
            .Include(x => x.Project)
            .Where(x => x.IsActive && !x.IsDeleted && x.Project.IsActive && !x.Project.IsDeleted)
            .ToListAsync(cancellationToken);
        var tasks = await _db.Tasks.AsNoTracking()
            .Include(x => x.Sprint).ThenInclude(x => x.Project)
            .Include(x => x.Assignments).ThenInclude(x => x.User)
            .Where(x => x.IsActive && !x.IsDeleted && x.Sprint.IsActive && !x.Sprint.IsDeleted && x.Sprint.Project.IsActive && !x.Sprint.Project.IsDeleted)
            .ToListAsync(cancellationToken);

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
        return $"{task.Title} — {task.Status}, {task.Priority} priority, {task.ProgressPercentage}% complete, {task.EstimateHours:0.##} hours. Start: {Date(task.StartDate)}; due: {Date(task.DueDate)}. Assignees: {string.Join(", ", assignees.DefaultIfEmpty("none"))}. Blocker: {task.BlockerInformation ?? "none recorded"}. Description: {task.Description}";
    }

    private static string AnswerSprint(string question, Sprint sprint, List<TaskItem> tasks)
    {
        var sprintTasks = tasks.Where(x => x.SprintId == sprint.Id).ToList();
        if (Has(question, "goal", "purpose")) return $"Sprint goal for {sprint.Name}: {sprint.Goal}";
        if (Has(question, "task", "work item")) return ListTasks(FilterTasks(question, sprintTasks));
        if (Has(question, "start", "end", "date", "deadline", "when"))
            return $"{sprint.Name} runs {Date(sprint.StartDate)} to {Date(sprint.EndDate)}.";
        if (Has(question, "status", "progress", "active", "complete"))
            return $"{sprint.Name} is {sprint.Status} with {sprintTasks.Count} tasks ({sprintTasks.Count(x => IsDone(x.Status))} complete).";
        return $"{sprint.Name} ({sprint.Project.Name}) — {sprint.Status}, {Date(sprint.StartDate)} to {Date(sprint.EndDate)}. Goal: {sprint.Goal}. {sprintTasks.Count} tasks.";
    }

    private static string AnswerProject(string question, Project project, List<Sprint> sprints, List<TaskItem> tasks)
    {
        if (Has(question, "count", "how many")) return $"{project.Name} has {sprints.Count} sprints and {tasks.Count} tasks.";
        if (Has(question, "description", "detail", "about"))
            return string.IsNullOrWhiteSpace(project.Description) ? $"{project.Name} has no description recorded." : $"{project.Name}: {project.Description}";
        if (Has(question, "task", "blocked", "priority", "assignee")) return ListTasks(FilterTasks(question, tasks));
        if (Has(question, "sprint"))
            return sprints.Count == 0 ? $"{project.Name} has no sprints." : string.Join("\n", sprints.Select(x => $"{x.Name}: {x.Status}, {Date(x.StartDate)} to {Date(x.EndDate)}"));
        if (Has(question, "near", "soon", "closest", "deadline", "end", "date", "when"))
            return ProjectDeadline(project);
        if (Has(question, "status", "active", "complete", "progress"))
            return $"{project.Name} is {project.Status}. It has {sprints.Count} sprints and {tasks.Count} tasks ({tasks.Count(x => IsDone(x.Status))} complete).";
        return $"{project.Name} — {project.Status}, {Date(project.StartDate)} to {Date(project.EndDate)}. {project.Description ?? "No description recorded."} {sprints.Count} sprints and {tasks.Count} tasks.";
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
            return string.Join("\n", ordered.Select(x => Has(question, "description", "about")
                ? $"{x.Name}: {x.Description ?? "No description recorded."}"
                : Has(question, "deadline", "date", "start", "when", "closest", "soonest", "next", "nearest")
                    ? $"{x.Name}: {x.Status}, starts {Date(x.StartDate)}, deadline {Date(x.EndDate)}"
                    : $"{x.Name}: {x.Status}"));
        }
        if (Has(question, "sprint"))
        {
            var matchingSprints = Has(question, "completed", "complete") ? sprints.Where(x => IsStatus(x.Status, "completed"))
                : Has(question, "active", "current") ? sprints.Where(x => IsStatus(x.Status, "active"))
                : sprints;
            var listedSprints = matchingSprints.ToList();
            return listedSprints.Count == 0 ? "There are no matching sprints." : string.Join("\n", listedSprints.Select(x => Has(question, "goal", "purpose")
                ? $"{x.Name} ({x.Project.Name}): {x.Goal}"
                : $"{x.Name} ({x.Project.Name}): {x.Status}, {Date(x.StartDate)} to {Date(x.EndDate)}"));
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
        var results = Has(question, "closest", "nearest") ? ordered.Take(5).ToList() : ordered.ToList();
        if (results.Count == 0)
            return Has(question, "overdue", "late") ? "No active projects are past their end date."
                : Has(question, "near", "soon", "upcoming") ? "No active projects have an end date in the next 30 days."
                : "No active projects have an end date set.";
        return string.Join("\n", results.Select(ProjectDeadline));
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
        var heading = tasks.Count > 20 ? $"Showing 20 of {tasks.Count} tasks.\n" : string.Empty;
        return heading + string.Join("\n", tasks.Take(20).Select(x =>
        {
            var assignees = x.Assignments.Where(a => a.IsActive && !a.IsDeleted && a.User.IsActive && !a.User.IsDeleted)
                .Select(a => $"{a.User.FirstName} {a.User.LastName}".Trim()).Distinct();
            return $"{x.Title} ({x.Sprint.Project.Name} / {x.Sprint.Name}) — {x.Status}, {x.Priority} priority, {x.ProgressPercentage}% complete, {x.EstimateHours:0.##} hours, due {Date(x.DueDate)}; assigned to {string.Join(", ", assignees.DefaultIfEmpty("nobody"))}; blocker: {x.BlockerInformation ?? "none"}.";
        }));
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
