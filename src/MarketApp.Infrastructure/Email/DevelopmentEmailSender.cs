using System.Text;
using MarketApp.Application.Interfaces.Services;
using Microsoft.Extensions.Hosting;

namespace MarketApp.Infrastructure.Email;

public class DevelopmentEmailSender : IApplicationEmailSender
{
    private readonly IHostEnvironment _environment;

    public DevelopmentEmailSender(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Configure a real email sender outside Development.");
        }

        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            throw new InvalidOperationException(
                "The local application-data folder is unavailable.");
        }

        var directory = Path.Combine(
            localAppData,
            "MarketApp",
            "DevelopmentMail");

        Directory.CreateDirectory(directory);

        var fileName =
            $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.txt";

        var filePath = Path.Combine(directory, fileName);

        var message = $"""
            To: {recipient}
            Subject: {subject}

            {body}
            """;

        await File.WriteAllTextAsync(
            filePath,
            message,
            Encoding.UTF8,
            cancellationToken);
    }
}