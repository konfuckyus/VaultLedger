using FluentAssertions;
using Moq;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

using VaultLedger.UnitTests.Helpers;

namespace VaultLedger.UnitTests.Services;

public class MultiCardRequestTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ICardRequestRepository> _cardRequests = new();
    private readonly Mock<ICardNumberGenerator> _generator = new();
    private readonly CardRequestService _sut;
    private readonly List<Card> _issuedCards = [];

    public MultiCardRequestTests()
    {
        _unitOfWork.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _unitOfWork.SetupGet(x => x.Cards).Returns(_cards.Object);
        _unitOfWork.SetupGet(x => x.CardRequests).Returns(_cardRequests.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _cards.Setup(x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()))
            .Callback<Card, CancellationToken>((c, _) => _issuedCards.Add(c))
            .Returns(Task.CompletedTask);

        _sut = new CardRequestService(_unitOfWork.Object, _generator.Object);
    }

    [Fact]
    public async Task TwoCardRequests_SameAccount_DifferentLabels_BothApprove_IndependentBlock()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var account = TestAccounts.CreateUser(userId, "2000000001");

        _accounts.Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Only Pending blocks; Active cards on the account do not.
        _cardRequests.Setup(x => x.HasPendingByUserAndAccountAsync(
                userId, account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request1 = await _sut.SubmitCardRequestAsync(userId, account.Id, "Yemek");
        request1.Label.Should().Be("Yemek");
        request1.Status.Should().Be(RequestStatus.Pending);

        _cardRequests.Setup(x => x.GetByIdAsync(request1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request1);
        _generator.Setup(x => x.GenerateUniqueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedCardNumber("hash-yemek", "1111", "4111111111111111"));

        var approve1 = await _sut.ApproveCardRequestAsync(request1.Id, adminId);
        approve1.Label.Should().Be("Yemek");
        request1.Status.Should().Be(RequestStatus.Approved);

        var card1 = _issuedCards.Should().ContainSingle().Subject;
        card1.Label.Should().Be("Yemek");
        card1.Status.Should().Be(CardStatus.Active);
        card1.AccountId.Should().Be(account.Id);

        var request2 = await _sut.SubmitCardRequestAsync(userId, account.Id, "Kurumsal");
        request2.Label.Should().Be("Kurumsal");
        request2.Status.Should().Be(RequestStatus.Pending);

        _cardRequests.Setup(x => x.GetByIdAsync(request2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request2);
        _generator.Setup(x => x.GenerateUniqueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedCardNumber("hash-kurumsal", "2222", "4222222222222222"));

        var approve2 = await _sut.ApproveCardRequestAsync(request2.Id, adminId);
        approve2.Label.Should().Be("Kurumsal");

        _issuedCards.Should().HaveCount(2);
        var card2 = _issuedCards[1];
        card2.Label.Should().Be("Kurumsal");
        card2.AccountId.Should().Be(account.Id);
        card2.Id.Should().NotBe(card1.Id);

        card1.Block();
        card1.Status.Should().Be(CardStatus.Blocked);
        card2.Status.Should().Be(CardStatus.Active);

        card2.Block();
        card2.Status.Should().Be(CardStatus.Blocked);
        card1.Unblock();
        card1.Status.Should().Be(CardStatus.Active);
        card2.Status.Should().Be(CardStatus.Blocked);
    }
}
