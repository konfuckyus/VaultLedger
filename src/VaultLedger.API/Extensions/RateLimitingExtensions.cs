using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace VaultLedger.API.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string TransactionsPolicy = "transactions";

    public static IServiceCollection AddVaultLedgerRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authLimit = configuration.GetValue("RateLimiting:AuthPermitLimit", 5);
        var authWindowSeconds = configuration.GetValue("RateLimiting:AuthWindowSeconds", 60);
        var txLimit = configuration.GetValue("RateLimiting:TransactionsPermitLimit", 30);
        var txWindowSeconds = configuration.GetValue("RateLimiting:TransactionsWindowSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = TimeSpan.FromSeconds(Math.Max(authWindowSeconds, txWindowSeconds));
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var metadataRetry))
                    retryAfter = metadataRetry;

                context.HttpContext.Response.Headers.RetryAfter =
                    Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                        .ToString(CultureInfo.InvariantCulture);

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Detail = "Rate limit exceeded. Retry after the period indicated by the Retry-After header."
                    },
                    cancellationToken);
            };

            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authLimit,
                        Window = TimeSpan.FromSeconds(authWindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(TransactionsPolicy, httpContext =>
            {
                var userKey =
                    httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = txLimit,
                        Window = TimeSpan.FromSeconds(txWindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }
}
