using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;
using System.Linq;

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

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null || user.IsDeleted ? null : user;
    }

    public async Task<User> UpdateRoleAsync(Guid id, string role, string updatedBy)
    {
        var user = await GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedBy;
        await _userRepository.UpdateAsync(user);
        return user;
    }

    public async Task DeleteUserAsync(Guid id, string deletedBy)
    {
        var user = await GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = deletedBy;
        await _userRepository.UpdateAsync(user);
    }
}
