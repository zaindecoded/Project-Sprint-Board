using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class AuthService
{
    private readonly UserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(UserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var user = await _userRepository.GetByEmailAsync(email.Trim());
        if (user == null || !VerifyPassword(user, password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive || user.IsDeleted)
            throw new UnauthorizedAccessException("User is inactive.");

        var expiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes());
        var token = GenerateJwtToken(user, expiresAt);

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = expiresAt
        };
    }

    public async Task<User> RegisterAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException("Email is required.");

        var existingEmail = await _userRepository.GetByEmailAsync(user.Email);
        if (existingEmail != null)
            throw new InvalidOperationException("Email already exists.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;
        user.IsDeleted = false;

        return await _userRepository.AddAsync(user);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }

    private static bool VerifyPassword(User user, string password)
    {
        if (user.PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal))
        {
            var hasher = new PasswordHasher<User>();
            return hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
        }

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    private string GenerateJwtToken(User user, DateTime expiresAt)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var configuredKey = jwtSettings["Key"];
        var keyValue = !string.IsNullOrWhiteSpace(configuredKey)
            ? configuredKey
            : Environment.GetEnvironmentVariable("TASKSPRINTBOARD_JWT_KEY");
        if (string.IsNullOrWhiteSpace(keyValue))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var key = Encoding.UTF8.GetBytes(keyValue);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = jwtSettings["Issuer"] ?? "TaskSprintBoard",
            Audience = jwtSettings["Audience"] ?? "TaskSprintBoardClients",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private int GetJwtExpirationMinutes()
    {
        var value = _configuration["Jwt:ExpirationMinutes"] ?? "60";
        return int.TryParse(value, out var minutes) && minutes > 0 ? minutes : 60;
    }
}
