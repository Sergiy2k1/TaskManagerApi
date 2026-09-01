using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Api.ErrorHandling;

public sealed partial class GlobalExceptionHandler
    : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) =
            MapException(exception);

        if (statusCode ==
            StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(
                _logger,
                exception,
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        await _problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception
            });

        return true;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message =
            "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}")]
    private static partial void LogUnhandledException(
        ILogger logger,
        Exception exception,
        string method,
        PathString path,
        string traceId);

    private static (
        int StatusCode,
        string Title,
        string Detail)
        MapException(Exception exception)
    {
        return exception switch
        {
            ApplicationValidationException =>
                (
                    StatusCodes.Status400BadRequest,
                    "Validation error",
                    exception.Message
                ),

            DomainValidationException =>
                (
                    StatusCodes.Status400BadRequest,
                    "Validation error",
                    exception.Message
                ),

            ApplicationUnauthorizedException =>
                (
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    exception.Message
                ),

            ApplicationForbiddenException =>
                (
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    exception.Message
                ),

            ApplicationNotFoundException =>
                (
                    StatusCodes.Status404NotFound,
                    "Not Found",
                    exception.Message
                ),

            ApplicationConflictException =>
                (
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    exception.Message
                ),

            DomainConflictException =>
                (
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    exception.Message
                ),

            _ =>
                (
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred."
                )
        };
    }
}