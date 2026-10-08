namespace MarketApp.Application.Persistence.Contracts;
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
