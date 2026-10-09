using System.Net;
using System.Net.Mail;
using MarketApp.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
namespace MarketApp.Infrastructure.Email;

// STARTTLS SMTP. Providers requiring OAuth or implicit TLS need their own adapter.
public sealed class SmtpEmailSender(IConfiguration configuration) : IApplicationEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        var settings = configuration.GetSection("Email");
        var host = settings["Host"] ?? throw new InvalidOperationException("Email:Host is required.");
        var from = settings["From"] ?? throw new InvalidOperationException("Email:From is required.");
        using var client = new SmtpClient(host, settings.GetValue("Port", 587)) { EnableSsl = true, Timeout = 30000 };
        if (!string.IsNullOrWhiteSpace(settings["UserName"]))
            client.Credentials = new NetworkCredential(settings["UserName"], settings["Password"]);
        using var message = new MailMessage(from, recipient, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
