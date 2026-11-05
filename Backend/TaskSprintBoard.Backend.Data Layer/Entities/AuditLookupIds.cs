namespace TaskSprintBoard.Backend.Data.Entities;

public static class AuditLookupIds
{
    public const int OtherAction = 9;
    public const int OtherEntity = 14;

    public static int Action(string name) => name switch
    {
        "Created" => 1,
        "Updated" => 2,
        "Deleted" => 3,
        "Status Changed" => 4,
        "Assigned" => 5,
        "Unassigned" => 6,
        "Assignment Updated" => 7,
        "Login" => 8,
        _ => OtherAction
    };

    public static int Entity(string name) => name switch
    {
        "Task" => 1,
        "Project" => 2,
        "Sprint" => 3,
        "User" => 4,
        "Assignment" => 5,
        "Comment" => 6,
        "TaskRiskEvaluation" => 7,
        "Role" => 8,
        "Status" => 9,
        "TaskPriorityLookup" => 10,
        "TaskStatusLookup" => 11,
        "RoleInTask" => 12,
        "Other" => 13,
        "Authentication" => 14,
        _ => OtherEntity
    };
}
