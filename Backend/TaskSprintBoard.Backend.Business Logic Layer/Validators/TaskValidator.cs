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
        if (!new[] { "Low", "Medium", "High", "Critical" }.Contains(priority, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid task priority.");
    }

    public static void ValidateStatus(string status)
    {
        if (!new[] { "Todo", "InProgress", "Blocked", "InReview", "Done", "Cancelled" }.Contains(status, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid task status.");
    }

    public static bool IsValidTransition(string oldStatus, string newStatus)
    {
        var valid = new Dictionary<string, string[]>
        {
            ["Todo"] = ["InProgress", "Cancelled"],
            ["InProgress"] = ["Blocked", "InReview", "Done", "Cancelled"],
            ["Blocked"] = ["InProgress", "Cancelled"],
            ["InReview"] = ["InProgress", "Done", "Cancelled"],
            ["Done"] = ["InProgress"],
            ["Cancelled"] = ["InProgress"]
        };

        var normalizedOldStatus = oldStatus?.Trim();
        var normalizedNewStatus = newStatus?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedOldStatus) || string.IsNullOrWhiteSpace(normalizedNewStatus))
            return false;

        var canonicalOld = valid.Keys.FirstOrDefault(x => string.Equals(x, normalizedOldStatus, StringComparison.OrdinalIgnoreCase));
        if (canonicalOld == null || !valid.TryGetValue(canonicalOld, out var allowed)) return false;

        return allowed.Any(item => string.Equals(item, normalizedNewStatus, StringComparison.OrdinalIgnoreCase));
    }
}
