using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class UserRepository
{
    private readonly TaskSprintBoardDbContext _context;
    private readonly DbSet<User> _users;

    public UserRepository(TaskSprintBoardDbContext context)
    {
        _context = context;
        _users = context.Set<User>();
    }

    public async Task<IEnumerable<User>> GetAllAsync() => await _users.AsNoTracking().ToListAsync();

    public Task<User?> GetByIdAsync(int id) => _users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<User?> GetByEmailAsync(string email) => await _users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted);

    public async Task<User?> GetByUserNameAsync(string userName) => await _users.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == userName && !x.IsDeleted);

    public async Task<User> AddAsync(User user)
    {
        await _users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _users.Update(user);
        await _context.SaveChangesAsync();
    }
}
