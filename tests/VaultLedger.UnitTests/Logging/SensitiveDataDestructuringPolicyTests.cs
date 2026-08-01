using FluentAssertions;
using Serilog.Events;
using VaultLedger.API.Logging;
using VaultLedger.Application.DTOs.Cards;
using VaultLedger.Domain.Entities;

namespace VaultLedger.UnitTests.Logging;

public class SensitiveDataDestructuringPolicyTests
{
    [Fact]
    public void User_PasswordHash_IsRedacted()
    {
        var policy = new SensitiveDataDestructuringPolicy();
        var user = User.Create("Test", "t@test.com", "super-secret-hash-value");

        var ok = policy.TryDestructure(user, new ScalarFactory(), out var result);

        ok.Should().BeTrue();
        var structure = result.Should().BeOfType<StructureValue>().Subject;
        var hashProp = structure.Properties.Single(p => p.Name == "PasswordHash");
        hashProp.Value.Should().BeOfType<ScalarValue>()
            .Which.Value.Should().Be("***REDACTED***");
        structure.Properties.Select(p => p.Value)
            .OfType<ScalarValue>()
            .Select(v => v.Value?.ToString())
            .Should().NotContain("super-secret-hash-value");
    }

    [Fact]
    public void Card_CardNumberHash_IsRedacted()
    {
        var policy = new SensitiveDataDestructuringPolicy();
        var card = Card.Issue(
            Guid.NewGuid(),
            "abc123hash",
            "4242",
            DateTime.UtcNow.AddYears(2));

        var ok = policy.TryDestructure(card, new ScalarFactory(), out var result);

        ok.Should().BeTrue();
        var structure = result.Should().BeOfType<StructureValue>().Subject;
        structure.Properties.Single(p => p.Name == "CardNumberHash").Value
            .Should().BeOfType<ScalarValue>().Which.Value.Should().Be("***REDACTED***");
        structure.Properties.Any(p => p.Name == "MaskedNumber").Should().BeTrue();
    }

    [Fact]
    public void ApproveCardRequestResult_RawCardNumber_IsRedacted()
    {
        var policy = new SensitiveDataDestructuringPolicy();
        var dto = new ApproveCardRequestResult
        {
            CardId = Guid.NewGuid(),
            LastFourDigits = "4242",
            MaskedNumber = "****4242",
            RawCardNumber = "4123456789012345"
        };

        var ok = policy.TryDestructure(dto, new ScalarFactory(), out var result);

        ok.Should().BeTrue();
        var structure = result.Should().BeOfType<StructureValue>().Subject;
        structure.Properties.Single(p => p.Name == "RawCardNumber").Value
            .Should().BeOfType<ScalarValue>().Which.Value.Should().Be("***REDACTED***");
        structure.Properties.Select(p => p.Value)
            .OfType<ScalarValue>()
            .Select(v => v.Value?.ToString())
            .Should().NotContain("4123456789012345");
    }

    private sealed class ScalarFactory : Serilog.Core.ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
            => new ScalarValue(value);
    }
}
