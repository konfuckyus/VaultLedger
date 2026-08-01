using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using VaultLedger.Application.Common;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Infrastructure.Services;

using VaultLedger.UnitTests.Helpers;

namespace VaultLedger.UnitTests.Services;

public class CardRequestServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ICardRequestRepository> _cardRequests = new();
    private readonly Mock<ICardNumberGenerator> _cardNumberGenerator = new();
    private readonly CardRequestService _sut;
    private readonly ICardNumberHasher _hasher;

    public CardRequestServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _unitOfWork.SetupGet(x => x.Cards).Returns(_cards.Object);
        _unitOfWork.SetupGet(x => x.CardRequests).Returns(_cardRequests.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _hasher = new CardNumberHasher(Options.Create(new CardHashOptions
        {
            Secret = "VaultLedger_UnitTest_CardHash_Secret_Key_32chars!"
        }));

        _sut = new CardRequestService(_unitOfWork.Object, _cardNumberGenerator.Object);
    }

    [Fact]
    public async Task SubmitCardRequestAsync_AccountOwnedByAnotherUser_ThrowsForbidden()
    {
        var requesterId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var account = TestAccounts.CreateUser(ownerId, "1000000003");

        _accounts.Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var act = () => _sut.SubmitCardRequestAsync(requesterId, account.Id);

        await act.Should().ThrowAsync<Application.Exceptions.ForbiddenException>()
            .WithMessage("*do not own*");
        _cardRequests.Verify(
            x => x.AddAsync(It.IsAny<CardRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveCardRequestAsync_ReturnsRawPan_MatchingStoredHash()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var account = TestAccounts.CreateUser(userId, "1000000004");
        var request = CardRequest.Create(userId, account.Id, hasPendingRequestForAccount: false);
        const string rawPan = "4123456789012345";
        var hash = _hasher.Hash(rawPan);
        var lastFour = _hasher.LastFour(rawPan);

        _cardRequests.Setup(x => x.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _cardNumberGenerator.Setup(x => x.GenerateUniqueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedCardNumber(hash, lastFour, rawPan));

        Card? savedCard = null;
        _cards.Setup(x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .Callback<Card, CancellationToken>((c, _) => savedCard = c)
            .Returns(Task.CompletedTask);

        var result = await _sut.ApproveCardRequestAsync(request.Id, adminId);

        result.RawCardNumber.Should().Be(rawPan);
        result.RawCardNumber.Should().HaveLength(16);
        result.RawCardNumber.Should().MatchRegex("^[0-9]{16}$");
        result.LastFourDigits.Should().Be(lastFour);
        result.MaskedNumber.Should().Be($"****{lastFour}");
        result.CardId.Should().Be(savedCard!.Id);
        savedCard.CardNumberHash.Should().Be(hash);
        _hasher.Hash(result.RawCardNumber).Should().Be(savedCard.CardNumberHash);
        request.Status.Should().Be(RequestStatus.Approved);
    }
}
