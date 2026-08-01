using FluentAssertions;
using Microsoft.AspNetCore.Http;
using VaultLedger.API.Middleware;

namespace VaultLedger.UnitTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task AddsBaselineSecurityHeaders()
    {
        var context = new DefaultHttpContext();

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Contain("default-src 'self'");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("no-referrer");
        context.Response.Headers["Permissions-Policy"].ToString().Should().Contain("camera=()");
    }
}
