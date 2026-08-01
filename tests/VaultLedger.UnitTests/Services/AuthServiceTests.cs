using FluentAssertions;
using Moq;
using VaultLedger.Application.DTOs.Auth;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

namespace VaultLedger.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(x => x.RefreshTokens).Returns(_refreshTokens.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _jwt.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), out It.Ref<DateTime>.IsAny))
            .Returns((User _, out DateTime exp) =>
            {
                exp = DateTime.UtcNow.AddMinutes(15);
                return "access-token";
            });
        _jwt.Setup(x => x.GenerateRefreshToken())
            .Returns(("raw-refresh", "refresh-hash", DateTime.UtcNow.AddDays(7)));
        _jwt.Setup(x => x.HashRefreshToken(It.IsAny<string>())).Returns("refresh-hash");

        _sut = new AuthService(_unitOfWork.Object, _hasher.Object, _jwt.Object);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var user = User.Create("Test User", "user@test.com", "stored-hash");
        _users.Setup(x => x.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.Verify("bad", "stored-hash")).Returns(false);

        var act = () => _sut.LoginAsync(new LoginRequestDto { Email = "user@test.com", Password = "bad" });

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorized()
    {
        var user = User.Create("System", "system@vaultledger.internal", "stored-hash");
        user.Deactivate();

        _users.Setup(x => x.GetByEmailAsync("system@vaultledger.internal", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.Verify("any", "stored-hash")).Returns(true);

        var act = () => _sut.LoginAsync(new LoginRequestDto
        {
            Email = "system@vaultledger.internal",
            Password = "any"
        });

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*inactive*");
        _refreshTokens.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        var user = User.Create("Test User", "user@test.com", "stored-hash", UserRole.User);
        _users.Setup(x => x.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(x => x.Verify("Secret123!", "stored-hash")).Returns(true);

        var result = await _sut.LoginAsync(new LoginRequestDto
        {
            Email = "user@test.com",
            Password = "Secret123!"
        });

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh");
        result.Email.Should().Be("user@test.com");
        result.Role.Should().Be("User");
        _refreshTokens.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_Rotates_RevokesOldAndIssuesNew()
    {
        var user = User.Create("Test User", "user@test.com", "stored-hash", UserRole.User);
        var stored = RefreshToken.Create(user.Id, "refresh-hash", DateTime.UtcNow.AddDays(7));

        _refreshTokens.Setup(x => x.GetByTokenHashAsync("refresh-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwt.Setup(x => x.GenerateRefreshToken())
            .Returns(("new-raw-refresh", "new-refresh-hash", DateTime.UtcNow.AddDays(7)));

        var result = await _sut.RefreshAsync(new RefreshTokenRequestDto { RefreshToken = "presented-raw" });

        stored.IsRevoked.Should().BeTrue();
        result.RefreshToken.Should().Be("new-raw-refresh");
        _refreshTokens.Verify(x => x.Update(stored), Times.Once);
        _refreshTokens.Verify(
            x => x.AddAsync(It.Is<RefreshToken>(t => t.TokenHash == "new-refresh-hash"), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_RevokedTokenReuse_RevokesAllSessions()
    {
        var user = User.Create("Test User", "user@test.com", "stored-hash", UserRole.User);
        var stored = RefreshToken.Create(user.Id, "refresh-hash", DateTime.UtcNow.AddDays(7));
        stored.Revoke();

        _refreshTokens.Setup(x => x.GetByTokenHashAsync("refresh-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var act = () => _sut.RefreshAsync(new RefreshTokenRequestDto { RefreshToken = "presented-raw" });

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*reuse*");
        _refreshTokens.Verify(
            x => x.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _refreshTokens.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
