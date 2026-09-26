namespace MarketApp.Application.Common.Exceptions;

public sealed class ConflictException : Exception
{
    public ConflictException(
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}