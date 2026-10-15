using System.Net.Http.Json;
using MarketApp.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketApp.Infrastructure.Email;

public sealed class BrevoEmailSender(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<BrevoEmailSender> logger) : IApplicationEmailSender
{
    public async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Email:ApiKey"];
        var from = configuration["Email:From"];
        var fromName = configuration["Email:FromName"] ?? "MarketApp";

        if (string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException(
                "Brevo requires Email:ApiKey and Email:From.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.brevo.com/v3/smtp/email");

        request.Headers.Add("api-key", apiKey);

        request.Content = JsonContent.Create(new
        {
            sender = new
            {
                name = fromName,
                email = from
            },
            to = new[]
            {
                new { email = recipient }
            },
            subject,
            textContent = body
        });

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Do not log credentials, recipients, or reset tokens.
            logger.LogError(
                "Brevo rejected the email request. HTTP status: {StatusCode}",
                (int)response.StatusCode);

            throw new HttpRequestException(
                "The email provider rejected the request.",
                null,
                response.StatusCode);
        }
    }
}