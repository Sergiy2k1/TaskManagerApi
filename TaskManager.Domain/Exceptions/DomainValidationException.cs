namespace TaskManager.Domain.Exceptions;

public sealed class DomainValidationException : DomainException
{
    public string? ParameterName { get; }

    public DomainValidationException(
        string message,
        string? parameterName = null)
        : base(message)
    {
        ParameterName = parameterName;
    }
}