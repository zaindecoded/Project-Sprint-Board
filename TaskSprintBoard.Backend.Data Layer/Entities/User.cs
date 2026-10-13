namespace TaskSprintBoard.Backend.Data.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "TeamMember";

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
