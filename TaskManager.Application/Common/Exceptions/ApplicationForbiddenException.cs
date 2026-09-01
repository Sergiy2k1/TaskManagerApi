namespace TaskManager.Application.Common.Exceptions;

public sealed class ApplicationForbiddenException
    : Exception
{
    public ApplicationForbiddenException(
        string message)
        : base(message)
    {
    }
}