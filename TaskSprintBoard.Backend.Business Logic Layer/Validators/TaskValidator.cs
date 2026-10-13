using TaskSprintBoard.Backend.BusinessLogic.Enums;

namespace TaskSprintBoard.Backend.BusinessLogic.Validators;

public static class TaskValidator
{
    public static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.");

        if (title.Trim().Length > 200)
            throw new ArgumentException("Task title cannot exceed 200 characters.");
    }

    public static void ValidateEstimate(decimal estimate)
    {
        if (estimate <= 0)
            throw new ArgumentException("Estimate must be greater than zero.");
    }

    public static void ValidateProgress(int progress)
    {
        if (progress < 0 || progress > 100)
            throw new ArgumentException("Progress must be between 0 and 100.");
    }

    public static void ValidatePriority(string priority)
    {
        if (!Enum.TryParse<TaskPriority>(priority, true, out _))
            throw new ArgumentException("Invalid task priority.");
    }

    public static void ValidateStatus(string status)
    {
        if (!Enum.TryParse<BusinessLogic.Enums.TaskStatus>(status, true, out _))
            throw new ArgumentException("Invalid task status.");
    }

    public static bool IsValidTransition(string oldStatus, string newStatus)
    {
        var valid = new Dictionary<string, string[]>
        {
            [nameof(BusinessLogic.Enums.TaskStatus.Todo)] = [nameof(BusinessLogic.Enums.TaskStatus.InProgress)],
            [nameof(BusinessLogic.Enums.TaskStatus.InProgress)] = [nameof(BusinessLogic.Enums.TaskStatus.Blocked), nameof(BusinessLogic.Enums.TaskStatus.InReview), nameof(BusinessLogic.Enums.TaskStatus.Done)],
            [nameof(BusinessLogic.Enums.TaskStatus.Blocked)] = [nameof(BusinessLogic.Enums.TaskStatus.InProgress)],
            [nameof(BusinessLogic.Enums.TaskStatus.InReview)] = [nameof(BusinessLogic.Enums.TaskStatus.InProgress), nameof(BusinessLogic.Enums.TaskStatus.Done)],
            [nameof(BusinessLogic.Enums.TaskStatus.Done)] = [nameof(BusinessLogic.Enums.TaskStatus.InProgress)],
            [nameof(BusinessLogic.Enums.TaskStatus.Cancelled)] = [nameof(BusinessLogic.Enums.TaskStatus.InProgress)]
        };

        var normalizedOldStatus = oldStatus?.Trim();
        var normalizedNewStatus = newStatus?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedOldStatus) || string.IsNullOrWhiteSpace(normalizedNewStatus))
            return false;

        if (!valid.TryGetValue(normalizedOldStatus, out var allowed))
        {
            if (!Enum.TryParse<BusinessLogic.Enums.TaskStatus>(normalizedOldStatus, true, out var oldStatusEnum))
                return false;

            normalizedOldStatus = oldStatusEnum.ToString();
            if (!valid.TryGetValue(normalizedOldStatus, out allowed))
                return false;
        }

        return allowed.Any(item => string.Equals(item, normalizedNewStatus, StringComparison.OrdinalIgnoreCase));
    }
}
