using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.BusinessLogic.Services;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Tests;

public class UserManagementTests
{
    private static TaskSprintBoardDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TaskSprintBoardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task AdminCannotDeactivateAnotherAdmin()
    {
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            FirstName = "Other", LastName = "Admin", Email = "other@example.test",
            UserName = "other-admin", RoleId = 1, IsActive = true
        });
        await context.SaveChangesAsync();
        var service = new UserService(new UserRepository(context));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteUserAsync(1, 2));
        Assert.True((await context.Users.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task AdminCanReactivateDeactivatedUser()
    {
        await using var context = CreateContext();
        var deactivatedUser = new User
        {
            FirstName = "Former", LastName = "Member", Email = "member@example.test",
            UserName = "former-member", IsActive = false, IsDeleted = true
        };
        context.Users.Add(deactivatedUser);
        await context.SaveChangesAsync();
        context.Entry(deactivatedUser).State = EntityState.Detached;
        var service = new UserService(new UserRepository(context));

        var user = await service.ActivateUserAsync(1, 9);

        Assert.True(user.IsActive);
        Assert.False(user.IsDeleted);
        Assert.Equal(9, user.UpdatedBy);
    }
}
