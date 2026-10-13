namespace TaskSprintBoard.Backend.BusinessLogic.Validators;

public static class ProjectValidator
{
    public static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name is required.");

        if (name.Trim().Length > 200)
            throw new ArgumentException("Project name cannot exceed 200 characters.");
    }
}
