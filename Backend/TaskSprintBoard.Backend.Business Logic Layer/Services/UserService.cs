using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class UserService
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException("Email is required.");

        var existingEmail = await _userRepository.GetByEmailAsync(user.Email);
        if (existingEmail != null)
            throw new InvalidOperationException("Email already exists.");

        var existingUserName = await _userRepository.GetByUserNameAsync(user.UserName);
        if (existingUserName != null)
            throw new InvalidOperationException("Username already exists.");

        var role = await ResolveRoleAsync(user.Role);
        user.RoleId = role.Id;
        user.RoleDefinition = role;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        return await _userRepository.AddAsync(user);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users
            .Where(user => user.IsActive && !user.IsDeleted)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.OrderBy(user => user.FirstName).ThenBy(user => user.LastName);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null || user.IsDeleted ? null : user;
    }

    public async Task<User> UpdateRoleAsync(int id, string role, int updatedBy)
    {
        var user = await GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
        var roleDefinition = await ResolveRoleAsync(role);
        user.RoleId = roleDefinition.Id;
        user.RoleDefinition = roleDefinition;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedBy;
        await _userRepository.UpdateAsync(user);
        return user;
    }

    public async Task<User> UpdateUserAsync(int id, string? firstName, string? lastName, string? email, string? userName, string? role, string? password, int updatedBy)
    {
        var user = await GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
        if (email != null && await _userRepository.GetByEmailForUpdateAsync(email, id) != null)
            throw new InvalidOperationException("Email already exists.");
        if (userName != null && await _userRepository.GetByUserNameForUpdateAsync(userName, id) != null)
            throw new InvalidOperationException("Username already exists.");
        if (firstName != null) user.FirstName = firstName;
        if (lastName != null) user.LastName = lastName;
        if (email != null) user.Email = email;
        if (userName != null) user.UserName = userName;
        if (role != null)
        {
            var roleDefinition = await ResolveRoleAsync(role);
            user.RoleId = roleDefinition.Id;
            user.RoleDefinition = roleDefinition;
        }
        if (!string.IsNullOrWhiteSpace(password)) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedBy;
        await _userRepository.UpdateAsync(user);
        return user;
    }

    public async Task DeleteUserAsync(int id, int deletedBy)
    {
        var user = await GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
        if (user.Role == "Admin" && id != deletedBy)
            throw new InvalidOperationException("You cannot deactivate another admin account.");
        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = deletedBy;
        await _userRepository.UpdateAsync(user);
    }

    public async Task<User> ActivateUserAsync(int id, int activatedBy)
    {
        var user = await _userRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
        user.IsDeleted = false;
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = activatedBy;
        await _userRepository.UpdateAsync(user);
        return user;
    }

    private async Task<Role> ResolveRoleAsync(string role)
    {
        var name = role switch { "ProjectManager" => "Project Manager", "TeamMember" => "Project Member", _ => role };
        return await _userRepository.GetRoleAsync(name)
            ?? throw new InvalidOperationException("Role must be one of the roles defined in the Roles table.");
    }
}
