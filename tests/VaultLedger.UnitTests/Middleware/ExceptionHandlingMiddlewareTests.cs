using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using VaultLedger.API.Middleware;
using VaultLedger.Application.Exceptions;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.UnitTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void Map_ReturnsExpectedStatusCode(Exception exception, int expectedStatus)
    {
        var (status, _, _, _) = ExceptionHandlingMiddleware.Map(exception);
        status.Should().Be(expectedStatus);
    }

    public static IEnumerable<object[]> Cases()
    {
        yield return [new NotFoundException("Account", Guid.NewGuid()), StatusCodes.Status404NotFound];
        yield return
        [
            new InsufficientBalanceException(Guid.NewGuid(), 10m, 1m),
            StatusCodes.Status422UnprocessableEntity
        ];
        yield return [new IdempotencyInProgressException("key-1"), StatusCodes.Status409Conflict];
        yield return [new ConcurrencyConflictException("conflict"), StatusCodes.Status409Conflict];
        yield return
        [
            new InvalidAccountOperationException("suspended"),
            StatusCodes.Status400BadRequest
        ];
        yield return [new UnauthorizedException("inactive"), StatusCodes.Status401Unauthorized];
        yield return [new ForbiddenException("no access"), StatusCodes.Status403Forbidden];
        yield return
        [
            new ValidationException(new Dictionary<string, string[]> { ["Email"] = ["required"] }),
            StatusCodes.Status400BadRequest
        ];
        yield return [new Exception("boom"), StatusCodes.Status500InternalServerError];
    }

    [Fact]
    public async Task InvokeAsync_WritesProblemDetailsJson()
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("User", Guid.NewGuid()),
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            env.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        context.Response.ContentType.Should().Be("application/problem+json");

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        doc.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
