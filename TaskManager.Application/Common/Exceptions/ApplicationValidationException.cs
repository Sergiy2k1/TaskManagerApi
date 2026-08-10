namespace TaskManager.Application.Common.Exceptions;

public sealed class ApplicationValidationException : Exception
{
    public string? ParameterName { get; }

    public ApplicationValidationException(
        string message,
        string? parameterName = null)
        : base(message)
    {
        ParameterName = parameterName;
    }
}