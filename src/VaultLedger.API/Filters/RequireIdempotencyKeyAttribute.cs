using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VaultLedger.API.Filters;

/// <summary>
/// Requires a non-empty GUID <c>Idempotency-Key</c> header for mutating financial endpoints.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireIdempotencyKeyAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var values)
            || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = $"Missing required '{HeaderName}' header."
            });
            return;
        }

        var raw = values.ToString().Trim();
        if (!Guid.TryParse(raw, out var parsed) || parsed == Guid.Empty)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = $"'{HeaderName}' must be a non-empty GUID (e.g. 550e8400-e29b-41d4-a716-446655440000)."
            });
            return;
        }

        // Canonical lowercase string form for consistent DB lookups.
        context.HttpContext.Items[HeaderName] = parsed.ToString("D");
        await next();
    }
}

public static class IdempotencyKeyHttpContextExtensions
{
    public static string GetIdempotencyKey(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(RequireIdempotencyKeyAttribute.HeaderName, out var value)
            && value is string key
            && !string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        throw new InvalidOperationException(
            $"{RequireIdempotencyKeyAttribute.HeaderName} was not captured. Apply [RequireIdempotencyKey].");
    }
}
