namespace MarketApp.Application.Interfaces.Services;

public interface IApplicationEmailSender
{
    Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}