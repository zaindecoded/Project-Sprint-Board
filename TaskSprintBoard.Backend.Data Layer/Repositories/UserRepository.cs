using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class UserRepository : GenericRepository<User>
{
    public UserRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == userName && !x.IsDeleted);
    }
}
