using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.Application.Exceptions;
using VaultLedger.Domain.Exceptions;
using AppValidationException = VaultLedger.Application.Exceptions.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace VaultLedger.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (status, title, detail, errors) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId={TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception {ExceptionType}. TraceId={TraceId}",
                exception.GetType().Name, traceId);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status >= 500 && !_environment.IsDevelopment()
                ? "An unexpected error occurred."
                : detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId
            }
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    public static (int Status, string Title, string Detail, IDictionary<string, string[]>? Errors) Map(
        Exception exception)
        => exception switch
        {
            NotFoundException ex =>
                (StatusCodes.Status404NotFound, "Not Found", ex.Message, null),

            InsufficientBalanceException ex =>
                (StatusCodes.Status422UnprocessableEntity, "Insufficient Balance",
                    string.IsNullOrWhiteSpace(ex.Message)
                        ? "Yetersiz bakiye: hesabınızda bu işlem için yeterli tutar yok."
                        : ex.Message,
                    null),

            IdempotencyInProgressException ex =>
                (StatusCodes.Status409Conflict, "Conflict", ex.Message, null),

            ConcurrencyConflictException ex =>
                (StatusCodes.Status409Conflict, "Conflict", ex.Message, null),

            InvalidAccountOperationException ex =>
                (StatusCodes.Status400BadRequest, "Bad Request", ex.Message, null),

            InvalidRequestOperationException ex =>
                (StatusCodes.Status400BadRequest, "Bad Request", ex.Message, null),

            InvalidAmountException ex =>
                (StatusCodes.Status400BadRequest, "Bad Request", ex.Message, null),

            InvalidPinException ex =>
                (StatusCodes.Status400BadRequest, "Bad Request", ex.Message, null),

            UnauthorizedException ex =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message, null),

            ForbiddenException ex =>
                (StatusCodes.Status403Forbidden, "Forbidden", ex.Message, null),

            AppValidationException ex =>
                (StatusCodes.Status400BadRequest, "Validation Failed", ex.Message, ex.Errors),

            FluentValidationException ex =>
            (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                "One or more validation errors occurred.",
                ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),

            _ =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error", exception.Message, null)
        };
}
