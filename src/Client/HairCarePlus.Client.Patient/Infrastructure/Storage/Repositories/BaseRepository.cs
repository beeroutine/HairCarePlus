using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HairCarePlus.Client.Patient.Infrastructure.Storage.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BaseRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected async Task<AppDbContext> GetContextAsync()
    {
        return await _dbFactory.CreateDbContextAsync();
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        await using var db = await GetContextAsync();
        return await db.Set<TEntity>().FindAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        await using var db = await GetContextAsync();
        return await db.Set<TEntity>().ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        await using var db = await GetContextAsync();
        return await db.Set<TEntity>().Where(predicate).ToListAsync();
    }

    public virtual async Task AddAsync(TEntity entity)
    {
        await using var db = await GetContextAsync();
        await db.Set<TEntity>().AddAsync(entity);
        await db.SaveChangesAsync();
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await using var db = await GetContextAsync();
        await db.Set<TEntity>().AddRangeAsync(entities);
        await db.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        await using var db = await GetContextAsync();
        db.Set<TEntity>().Update(entity);
        await db.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        await using var db = await GetContextAsync();
        db.Set<TEntity>().Remove(entity);
        await db.SaveChangesAsync();
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities)
    {
        await using var db = await GetContextAsync();
        db.Set<TEntity>().RemoveRange(entities);
        await db.SaveChangesAsync();
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        await using var db = await GetContextAsync();
        return await db.Set<TEntity>().AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
    {
        await using var db = await GetContextAsync();
        return await db.Set<TEntity>().CountAsync(predicate);
    }
} 