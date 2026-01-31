namespace AutoMerge.Infrastructure.AI;

public sealed class AiServiceException : Exception
{
    public AiServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
