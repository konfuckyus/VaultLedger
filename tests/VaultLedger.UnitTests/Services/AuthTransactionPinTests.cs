using FluentAssertions;
using Moq;
using VaultLedger.Application.DTOs.Auth;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.UnitTests.Services;

public class AuthTransactionPinTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly AuthService _sut;

    public AuthTransactionPinTests()
    {
        _unitOfWork.SetupGet(x => x.Users).Returns(_users.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sut = new AuthService(_unitOfWork.Object, _hasher.Object, _jwt.Object);
    }

    [Fact]
    public async Task SetTransactionPinAsync_FirstTime_HashesAndStores()
    {
        var user = User.Create("Test", "a@b.com", "hash", UserRole.User);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(x => x.Hash("1234")).Returns("pin-hash");

        await _sut.SetTransactionPinAsync(user.Id, new SetTransactionPinDto { Pin = "1234" });

        user.HasTransactionPin.Should().BeTrue();
        user.TransactionPinHash.Should().Be("pin-hash");
        _users.Verify(x => x.Update(user), Times.Once);
    }

    [Fact]
    public async Task SetTransactionPinAsync_ChangeWithoutOldPin_Throws()
    {
        var user = User.Create("Test", "a@b.com", "hash", UserRole.User);
        user.SetTransactionPinHash("existing");
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var act = () => _sut.SetTransactionPinAsync(
            user.Id, new SetTransactionPinDto { Pin = "5678" });

        await act.Should().ThrowAsync<InvalidPinException>();
    }

    [Fact]
    public async Task SetTransactionPinAsync_ChangeWithWrongOldPin_Throws()
    {
        var user = User.Create("Test", "a@b.com", "hash", UserRole.User);
        user.SetTransactionPinHash("existing");
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(x => x.Verify("0000", "existing")).Returns(false);

        var act = () => _sut.SetTransactionPinAsync(
            user.Id, new SetTransactionPinDto { Pin = "5678", OldPin = "0000" });

        await act.Should().ThrowAsync<InvalidPinException>();
    }
}
