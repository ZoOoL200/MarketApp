namespace MarketApp.Application.Common.Exceptions;

public sealed class RequestException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
