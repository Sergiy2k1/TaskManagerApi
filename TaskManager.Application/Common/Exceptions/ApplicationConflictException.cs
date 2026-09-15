namespace TaskManager.Application.Common.Exceptions;

public sealed class ApplicationConflictException : Exception
{
    public ApplicationConflictException(string message)
        : base(message)
    {
    }

    public ApplicationConflictException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
