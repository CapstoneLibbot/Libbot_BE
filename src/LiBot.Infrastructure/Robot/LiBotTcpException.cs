namespace LiBot.Infrastructure.Robot;

public enum LiBotTcpFailure
{
    ConnectionRefused,
    ConnectTimeout,
    ResponseTimeout,
    NetworkError,
    InvalidJsonResponse,
    EmptyResponse,
    ResponseTooLarge
}

public sealed class LiBotTcpException : Exception
{
    public LiBotTcpException(
        LiBotTcpFailure failure,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public LiBotTcpFailure Failure { get; }
}
