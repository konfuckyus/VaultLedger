using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using VaultLedger.API.Filters;

namespace VaultLedger.UnitTests.Filters;

public class RequireIdempotencyKeyAttributeTests
{
    [Fact]
    public async Task MissingHeader_Returns400()
    {
        var context = CreateContext(headerValue: null);
        var attribute = new RequireIdempotencyKeyAttribute();

        await attribute.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));

        var result = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task EmptyHeader_Returns400()
    {
        var context = CreateContext("   ");
        var attribute = new RequireIdempotencyKeyAttribute();

        await attribute.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));

        context.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task NonGuidHeader_Returns400()
    {
        var context = CreateContext("not-a-guid");
        var attribute = new RequireIdempotencyKeyAttribute();

        await attribute.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));

        var result = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Detail.Should().Contain("GUID");
    }

    [Fact]
    public async Task EmptyGuid_Returns400()
    {
        var context = CreateContext(Guid.Empty.ToString("D"));
        var attribute = new RequireIdempotencyKeyAttribute();

        await attribute.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));

        context.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ValidGuid_StoresCanonicalKeyAndContinues()
    {
        var id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var context = CreateContext(id.ToString("D").ToUpperInvariant());
        var attribute = new RequireIdempotencyKeyAttribute();
        var nextCalled = false;

        await attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                controller: null!));
        });

        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
        context.HttpContext.GetIdempotencyKey().Should().Be(id.ToString("D"));
    }

    private static ActionExecutingContext CreateContext(string? headerValue)
    {
        var httpContext = new DefaultHttpContext();
        if (headerValue is not null)
            httpContext.Request.Headers[RequireIdempotencyKeyAttribute.HeaderName] = headerValue;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: null!);
    }
}
