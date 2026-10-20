using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class GenericRepository<T> where T : BaseEntity
{
    protected readonly TaskSprintBoardDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(TaskSprintBoardDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null)
    {
        IQueryable<T> query = DbSet.AsNoTracking();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        var trackedEntity = Context.Set<T>().Local.FirstOrDefault(x => x.Id == entity.Id);

        if (trackedEntity != null && !ReferenceEquals(trackedEntity, entity))
        {
            Context.Entry(trackedEntity).CurrentValues.SetValues(entity);
            Context.Entry(trackedEntity).State = EntityState.Modified;
            await Context.SaveChangesAsync();
            return;
        }

        DbSet.Update(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await DbSet.FirstOrDefaultAsync(x => x.Id == id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            DbSet.Update(entity);
            await Context.SaveChangesAsync();
        }
    }
}
