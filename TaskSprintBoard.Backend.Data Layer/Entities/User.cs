using System.ComponentModel.DataAnnotations.Schema;

namespace TaskSprintBoard.Backend.Data.Entities;

public class User
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    [NotMapped]
    public string Role
    {
        get => RoleId switch { 1 => "Admin", 2 => "ProjectManager", _ => "TeamMember" };
        set => RoleId = value switch { "Admin" => 1, "ProjectManager" or "Project Manager" => 2, "TeamMember" or "Project Member" => 3, _ => RoleId };
    }
    public int RoleId { get; set; } = 3;
    public Role? RoleDefinition { get; set; }

    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
}
