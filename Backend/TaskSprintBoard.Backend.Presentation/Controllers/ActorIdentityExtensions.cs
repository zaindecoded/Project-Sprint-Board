using System.Security.Claims;

namespace TaskSprintBoard.Backend.Presentation.Controllers;

internal static class ActorIdentityExtensions
{
    public static int GetActorUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!int.TryParse(value, out var userId) || userId <= 0)
            throw new UnauthorizedAccessException("The token does not identify a valid user.");
        return userId;
    }
}
