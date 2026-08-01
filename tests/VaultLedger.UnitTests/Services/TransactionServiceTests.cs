using FluentAssertions;
using Moq;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

using VaultLedger.UnitTests.Helpers;

namespace VaultLedger.UnitTests.Services;

public class TransactionServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<ILedgerEntryRepository> _ledgerEntries = new();
    private readonly Mock<ITransactionRecordRepository> _transactionRecords = new();
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _unitOfWork.SetupGet(x => x.Cards).Returns(_cards.Object);
        _unitOfWork.SetupGet(x => x.LedgerEntries).Returns(_ledgerEntries.Object);
        _unitOfWork.SetupGet(x => x.TransactionRecords).Returns(_transactionRecords.Object);

        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new TransactionService(_unitOfWork.Object);
    }

    [Fact]
    public async Task SpendAsync_SameIdempotencyKey_ReturnsExistingCompletedRecord_WithoutNewLedgerEntries()
    {
        var userAccountId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        const string key = "spend-key-1";
        var existing = TransactionRecord.Create(
            TransactionType.Spend,
            userAccountId,
            SystemAccounts.ClearingAccountId,
            25m,
            Guid.NewGuid(),
            key,
            cardId);
        existing.MarkCompleted();

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.SpendAsync(userAccountId, cardId, 25m, key);

        result.Should().BeSameAs(existing);
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cards.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _ledgerEntries.Verify(
            x => x.AddAsync(It.IsAny<LedgerEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SpendAsync_InsufficientBalance_ThrowsInsufficientBalanceException()
    {
        var userAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var user = CreateUserAccountWithId(userAccountId, openingBalance: 10m);
        var system = CreateSystemAccount();
        var card = CreateActiveCard(userAccountId);

        SetupNoExistingTxn();
        SetupCard(card);
        SetupAccountLocks(userAccountId, user, system);

        var act = () => _sut.SpendAsync(userAccountId, card.Id, 100m, "spend-insufficient");

        await act.Should().ThrowAsync<InsufficientBalanceException>();
        _unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _ledgerEntries.Verify(
            x => x.AddAsync(It.IsAny<LedgerEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SpendAsync_WithSufficientBalance_WritesDebitAndCreditLedgerEntries()
    {
        var userAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var user = CreateUserAccountWithId(userAccountId, openingBalance: 100m);
        var system = CreateSystemAccount();
        var card = CreateActiveCard(userAccountId);
        const string key = "spend-ok-1";

        SetupNoExistingTxn(key);
        SetupCard(card);
        SetupAccountLocks(userAccountId, user, system);

        var result = await _sut.SpendAsync(userAccountId, card.Id, 40m, key, "Coffee");

        result.Status.Should().Be(TransactionStatus.Completed);
        result.Type.Should().Be(TransactionType.Spend);
        result.CardId.Should().Be(card.Id);
        user.Balance.Should().Be(60m);
        system.Balance.Should().Be(40m);

        _ledgerEntries.Verify(
            x => x.AddAsync(
                It.Is<LedgerEntry>(e => e.EntryType == EntryType.Debit && e.IdempotencyKey == $"{key}:debit"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SpendAsync_WhenCreditLedgerAddFails_RollsBack_AndDoesNotCommitSuccessPath()
    {
        var userAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var user = CreateUserAccountWithId(userAccountId, openingBalance: 100m);
        var system = CreateSystemAccount();
        var card = CreateActiveCard(userAccountId);
        const string key = "spend-atomic-1";

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionRecord?)null);

        SetupCard(card);
        SetupAccountLocks(userAccountId, user, system);

        _ledgerEntries
            .Setup(x => x.AddAsync(
                It.Is<LedgerEntry>(e => e.EntryType == EntryType.Debit),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _ledgerEntries
            .Setup(x => x.AddAsync(
                It.Is<LedgerEntry>(e => e.EntryType == EntryType.Credit),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated credit write failure"));

        var act = () => _sut.SpendAsync(userAccountId, card.Id, 40m, key);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated credit write failure");

        _unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SpendAsync_BlockedCard_IsRejected()
    {
        var userAccountId = Guid.NewGuid();
        var card = CreateActiveCard(userAccountId);
        card.Block();

        SetupNoExistingTxn();
        SetupCard(card);

        var act = () => _sut.SpendAsync(userAccountId, card.Id, 10m, "spend-blocked");

        await act.Should().ThrowAsync<InvalidAccountOperationException>()
            .WithMessage("*bloke*");
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SpendAsync_CardBelongsToAnotherAccount_ThrowsForbidden()
    {
        var userAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var card = CreateActiveCard(otherAccountId);

        SetupNoExistingTxn();
        SetupCard(card);

        var act = () => _sut.SpendAsync(userAccountId, card.Id, 10m, "spend-wrong-account");

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*bu hesaba ait değil*");
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SpendAsync_ExpiredCard_IsRejected()
    {
        var userAccountId = Guid.NewGuid();
        var card = CreateActiveCard(userAccountId);
        typeof(Card).GetProperty(nameof(Card.ExpiresAt))!
            .SetValue(card, DateTime.UtcNow.AddDays(-1));

        SetupNoExistingTxn();
        SetupCard(card);

        var act = () => _sut.SpendAsync(userAccountId, card.Id, 10m, "spend-expired");

        await act.Should().ThrowAsync<InvalidAccountOperationException>()
            .WithMessage("*bloke*");
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupNoExistingTxn(string? key = null)
    {
        if (key is null)
        {
            _transactionRecords
                .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactionRecord?)null);
        }
        else
        {
            _transactionRecords
                .Setup(x => x.GetByIdempotencyKeyAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactionRecord?)null);
        }
    }

    private void SetupCard(Card card)
    {
        _cards.Setup(x => x.GetByIdAsync(card.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);
    }

    private void SetupAccountLocks(Guid userAccountId, Account user, Account system)
    {
        _accounts
            .Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == SystemAccounts.ClearingAccountId ? system :
                id == userAccountId ? user : null);
    }

    private static Card CreateActiveCard(Guid accountId)
    {
        var card = Card.Issue(
            accountId,
            "hash-" + Guid.NewGuid().ToString("N"),
            "4242",
            DateTime.UtcNow.AddYears(2));
        return card;
    }

    private static Account CreateUserAccountWithId(Guid accountId, decimal openingBalance)
    {
        var account = TestAccounts.CreateUser(Guid.NewGuid(), "1000000001");
        typeof(BaseEntity)
            .GetProperty(nameof(BaseEntity.Id))!
            .SetValue(account, accountId);

        if (openingBalance > 0m)
            account.Credit(openingBalance);

        return account;
    }

    private static Account CreateSystemAccount()
        => Account.CreateSystemClearing(SystemAccounts.SystemUserId);
}
