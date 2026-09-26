using System.Linq.Expressions;
using MarketApp.Application.Common;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Infrastructure.Repositories;

/// <summary>
/// A generic repository implementation for performing CRUD operations on entities of type T.
/// </summary>
/// <typeparam name="T"></typeparam>
public class GeneralRepository<T> : IGeneralRepository<T>
    where T : class
{
    private readonly DbSet<T> _dbSet;

    public GeneralRepository(AppDbContext context)
    {
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Builds a queryable collection of entities of type T based on the provided predicate, includes, and tracking options.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="includes"></param>
    /// <param name="trackChanges"></param>
    /// <returns></returns>
    private IQueryable<T> BuildQuery(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, object>>[]? includes,
        bool trackChanges)
    {
        IQueryable<T> query = trackChanges
            ? _dbSet.AsTracking()
            : _dbSet.AsNoTracking();

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        if (includes is not null)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }

        return query;
    }

    /// <summary>
    /// Finds a single entity of type T that matches the provided predicate, optionally including related entities and tracking changes.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="includes"></param>
    /// <param name="trackChanges"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<T?> FindAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, object>>[]? includes = null,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await BuildQuery(predicate, includes, trackChanges)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of entities of type T that match the provided predicate, optionally including related entities, ordering the results, and tracking changes.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="orderBy"></param>
    /// <param name="includes"></param>
    /// <param name="trackChanges"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, object>>[]? includes = null,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(predicate, includes, trackChanges);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a paginated list of entities of type T that match the provided predicate, ordered by the specified orderBy function, optionally including related entities and tracking changes.
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="orderBy"></param>
    /// <param name="predicate"></param>
    /// <param name="includes"></param>
    /// <param name="trackChanges"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<PagedResult<T>> GetPageAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>[]? includes = null,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderBy);

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "Page number must be at least 1.");
        }

        if (pageSize < 1 || pageSize > 200)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be between 1 and 200.");
        }

        var offset = ((long)pageNumber - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "The requested page number is too large.");
        }

        // Count matching records without loading related entities.
        var totalCount = await BuildQuery(
                predicate,
                includes: null,
                trackChanges: false)
            .CountAsync(cancellationToken);

        var query = BuildQuery(predicate, includes, trackChanges);

        var items = await orderBy(query)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(
            items,
            totalCount,
            pageNumber,
            pageSize);
    }

    /// <summary>
    /// Checks if any entities of type T match the provided predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _dbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Adds a new entity of type T to the database context.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _dbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Adds a range of entities of type T to the database context.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// Removes an entity of type T from the database context.
    /// </summary>
    /// <param name="entity"></param>
    public void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _dbSet.Remove(entity);
    }

    /// <summary>
    /// Removes a range of entities of type T from the database context.
    /// </summary>
    /// <param name="entities"></param>
    public void RemoveRange(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _dbSet.RemoveRange(entities);
    }
}