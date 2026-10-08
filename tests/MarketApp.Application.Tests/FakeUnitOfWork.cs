using System.Linq.Expressions;
using MarketApp.Application.Common;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Entity.Pricing;
using MarketApp.Domain.Entity.Purchasing;

namespace MarketApp.Application.Tests;

// These fakes exercise application decisions, not database transactions or xmin.
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];
    public int SaveCount { get; private set; }

    public FakeRepository<T> Repo<T>() where T : class
    {
        if (!_repositories.TryGetValue(typeof(T), out var repository))
        {
            repository = new FakeRepository<T>();
            _repositories.Add(typeof(T), repository);
        }
        return (FakeRepository<T>)repository;
    }

    public IGeneralRepository<Category> Categories => Repo<Category>();
    public IGeneralRepository<Product> Products => Repo<Product>();
    public IGeneralRepository<Branch> Branches => Repo<Branch>();
    public IGeneralRepository<Supplier> Suppliers => Repo<Supplier>();
    public IGeneralRepository<InventoryLocation> InventoryLocations => Repo<InventoryLocation>();
    public IGeneralRepository<StockBalance> StockBalances => Repo<StockBalance>();
    public IGeneralRepository<PurchaseInvoice> PurchaseInvoices => Repo<PurchaseInvoice>();
    public IGeneralRepository<StockMovement> StockMovements => Repo<StockMovement>();
    public IGeneralRepository<StockTransfer> StockTransfers => Repo<StockTransfer>();
    public IGeneralRepository<BranchProductPrice> BranchProductPrices => Repo<BranchProductPrice>();
    public IGeneralRepository<BranchProductPriceHistory> BranchProductPriceHistories => Repo<BranchProductPriceHistory>();

    public IGeneralRepository<MarketApp.Domain.Entity.Sales.SalesInvoice> SalesInvoices => Repo<MarketApp.Domain.Entity.Sales.SalesInvoice>();
    public Task<IUnitOfWorkTransaction> BeginSerializableAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Use integration tests for commerce transactions.");
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeRepository<T> : IGeneralRepository<T> where T : class
{
    public List<T> Items { get; } = [];

    public Task<T?> FindAsync(Expression<Func<T, bool>> predicate,
        Expression<Func<T, object>>[]? includes = null, bool trackChanges = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(predicate.Compile()));

    public Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, object>>[]? includes = null, bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        var query = Items.AsQueryable();
        if (predicate is not null) query = query.Where(predicate);
        if (orderBy is not null) query = orderBy(query);
        return Task.FromResult<IReadOnlyList<T>>(query.ToList());
    }

    public async Task<PagedResult<T>> GetPageAsync(int pageNumber, int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>[]? includes = null, bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        var items = await GetAllAsync(predicate, orderBy, includes, trackChanges, cancellationToken);
        return new(items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
            items.Count, pageNumber, pageSize);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(predicate.Compile()));

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        Items.AddRange(entities);
        return Task.CompletedTask;
    }

    public void Remove(T entity) => Items.Remove(entity);
    public void RemoveRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities.ToArray()) Items.Remove(entity);
    }
}
