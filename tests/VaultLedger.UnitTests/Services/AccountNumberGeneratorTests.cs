using FluentAssertions;
using Moq;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Infrastructure.Services;

namespace VaultLedger.UnitTests.Services;

public class AccountNumberGeneratorTests
{
    [Fact]
    public async Task GenerateUniqueAsync_RetriesOnCollision_ReturnsThirdCandidate()
    {
        var accounts = new Mock<IAccountRepository>();
        var candidates = new List<string>();

        accounts.Setup(x => x.ExistsByAccountNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string number, CancellationToken _) =>
            {
                candidates.Add(number);
                // First two collide; third is free.
                return candidates.Count < 3;
            });

        var sut = new AccountNumberGenerator(accounts.Object);

        var result = await sut.GenerateUniqueAsync();

        candidates.Should().HaveCount(3);
        result.Should().Be(candidates[2]);
        result.Should().HaveLength(10);
        result.Should().MatchRegex("^[0-9]{10}$");
        accounts.Verify(
            x => x.ExistsByAccountNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }
}
