using System.Linq.Expressions;
using MarketApp.Application.Common;

namespace MarketApp.Application.Persistence.Contracts;

/// <summary>
/// Defines a generic repository for performing CRUD operations on entities of type T.
/// </summary>
/// <typeparam name="T">The type of entity to operate on.</typeparam>
public interface IGeneralRepository<T> where T : class
{
    /// <summary>
    /// Finds an entity of type T that matches the specified predicate, with optional includes and tracking behavior.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="includes"></param>
    /// <param name="trackChanges"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<T?> FindAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, object>>[]? includes = null,
        bool trackChanges = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of entities of type T that match the specified predicate, with optional ordering, includes, and tracking behavior.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="orderBy"></param>
    /// <param name="includes"></param>
    /// <param name="trackChanges"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, object>>[]? includes = null,
        bool trackChanges = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated result of entities of type T that match the specified predicate, with ordering, includes, and tracking behavior.
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="orderBy"></param>
    /// <param name="predicate"></param>
    /// <param name="includes"></param>
    /// <param name="trackChanges"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<PagedResult<T>> GetPageAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>[]? includes = null,
        bool trackChanges = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity of type T exists that matches the specified predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity of type T to the repository asynchronously.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a range of entities of type T to the repository asynchronously.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity of type T from the repository.
    /// </summary>
    /// <param name="entity"></param>
    void Remove(T entity);

    /// <summary>
    /// Removes a range of entities of type T from the repository.
    /// </summary>
    /// <param name="entities"></param>
    void RemoveRange(IEnumerable<T> entities);
}