using FluentAssertions;
using Moq;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

using VaultLedger.UnitTests.Helpers;

namespace VaultLedger.UnitTests.Services;

public class TransactionServiceTransferTests
{
    // A < B via Guid.CompareTo (byte-order), never string comparison.
    private static readonly Guid AccountAId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid AccountBId = Guid.Parse("11111111-0000-0000-0000-000000000002");

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ILedgerEntryRepository> _ledgerEntries = new();
    private readonly Mock<ITransactionRecordRepository> _transactionRecords = new();
    private readonly TransactionService _sut;

    public TransactionServiceTransferTests()
    {
        AccountAId.CompareTo(AccountBId).Should().BeNegative(
            "fixture Guids must satisfy A < B for lock-order scenarios");

        _unitOfWork.SetupGet(x => x.Accounts).Returns(_accounts.Object);
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
    public async Task TransferAsync_A_to_B_LocksSmallerIdFirst_ThenLargerId()
    {
        // Risk under test: even when source=A (smaller), lock order must be A then B
        // (ascending Guid.CompareTo), not "source then destination" as a coincidence.
        var accountA = CreateUserAccountWithId(AccountAId, 100m);
        var accountB = CreateUserAccountWithId(AccountBId, 100m);

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionRecord?)null);

        var sequence = new MockSequence();
        _accounts.InSequence(sequence)
            .Setup(x => x.GetByIdForUpdateAsync(AccountAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountA);
        _accounts.InSequence(sequence)
            .Setup(x => x.GetByIdForUpdateAsync(AccountBId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountB);

        var result = await _sut.TransferAsync(AccountAId, AccountBId, 10m, "xfer-a-to-b");

        result.Type.Should().Be(TransactionType.Transfer);
        accountA.Balance.Should().Be(90m);
        accountB.Balance.Should().Be(110m);

        _accounts.Verify(
            x => x.GetByIdForUpdateAsync(AccountAId, It.IsAny<CancellationToken>()),
            Times.Once);
        _accounts.Verify(
            x => x.GetByIdForUpdateAsync(AccountBId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TransferAsync_B_to_A_StillLocksSmallerIdFirst_ThenLargerId()
    {
        // Real deadlock risk: source=B (larger), destination=A (smaller).
        // Role-based "lock source then dest" would lock B then A — opposite of A→B —
        // and concurrent A→B + B→A would deadlock. We require the SAME order: A then B.
        var accountA = CreateUserAccountWithId(AccountAId, 100m);
        var accountB = CreateUserAccountWithId(AccountBId, 100m);

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionRecord?)null);

        var sequence = new MockSequence();
        _accounts.InSequence(sequence)
            .Setup(x => x.GetByIdForUpdateAsync(AccountAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountA);
        _accounts.InSequence(sequence)
            .Setup(x => x.GetByIdForUpdateAsync(AccountBId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountB);

        var result = await _sut.TransferAsync(AccountBId, AccountAId, 10m, "xfer-b-to-a");

        result.Type.Should().Be(TransactionType.Transfer);
        // Source B debited, destination A credited
        accountB.Balance.Should().Be(90m);
        accountA.Balance.Should().Be(110m);

        _accounts.Verify(
            x => x.GetByIdForUpdateAsync(AccountAId, It.IsAny<CancellationToken>()),
            Times.Once);
        _accounts.Verify(
            x => x.GetByIdForUpdateAsync(AccountBId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TransferAsync_IdempotencyCheck_RunsBeforeBeginTransaction_AndSkipsLocks()
    {
        const string key = "xfer-idempotent";
        var existing = TransactionRecord.Create(
            TransactionType.Transfer,
            AccountAId,
            AccountBId,
            15m,
            Guid.NewGuid(),
            key);
        existing.MarkCompleted();

        var callOrder = new List<string>();

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(key, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("idempotency"))
            .ReturnsAsync(existing);

        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("begin"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransferAsync(AccountAId, AccountBId, 15m, key);

        result.Should().BeSameAs(existing);
        callOrder.Should().Equal("idempotency");
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _accounts.Verify(
            x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _ledgerEntries.Verify(
            x => x.AddAsync(It.IsAny<LedgerEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TransferAsync_HappyPath_IdempotencyThenBeginThenLocks()
    {
        var accountA = CreateUserAccountWithId(AccountAId, 0m);
        var accountB = CreateUserAccountWithId(AccountBId, 50m);
        var callOrder = new List<string>();

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("idempotency"))
            .ReturnsAsync((TransactionRecord?)null);

        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("begin"))
            .Returns(Task.CompletedTask);

        _accounts
            .Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback((Guid id, CancellationToken _) => callOrder.Add($"lock:{id}"))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == AccountAId ? accountA :
                id == AccountBId ? accountB : null);

        await _sut.TransferAsync(AccountBId, AccountAId, 10m, "xfer-order");

        callOrder.Should().Equal(
            "idempotency",
            "begin",
            $"lock:{AccountAId}",
            $"lock:{AccountBId}");
    }

    [Fact]
    public async Task TransferAsync_SelfTransfer_ThrowsInvalidAccountOperationException()
    {
        var act = () => _sut.TransferAsync(AccountAId, AccountAId, 10m, "xfer-self");

        await act.Should().ThrowAsync<InvalidAccountOperationException>()
            .WithMessage("*must differ*");
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_SuspendedDestination_ThrowsInvalidAccountOperationException()
    {
        var source = CreateUserAccountWithId(AccountAId, 50m);
        var destination = CreateUserAccountWithId(AccountBId, 0m);
        destination.Suspend();

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionRecord?)null);

        _accounts
            .Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == AccountAId ? source :
                id == AccountBId ? destination : null);

        var act = () => _sut.TransferAsync(AccountAId, AccountBId, 10m, "xfer-suspended-dest");

        await act.Should().ThrowAsync<InvalidAccountOperationException>()
            .WithMessage("*Suspended*");
        _unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _ledgerEntries.Verify(
            x => x.AddAsync(It.IsAny<LedgerEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TransferAsync_SuspendedSource_ThrowsInvalidAccountOperationException()
    {
        var source = CreateUserAccountWithId(AccountAId, 50m);
        var destination = CreateUserAccountWithId(AccountBId, 0m);
        source.Suspend();

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionRecord?)null);

        _accounts
            .Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == AccountAId ? source :
                id == AccountBId ? destination : null);

        var act = () => _sut.TransferAsync(AccountAId, AccountBId, 10m, "xfer-suspended-source");

        await act.Should().ThrowAsync<InvalidAccountOperationException>()
            .WithMessage("*Suspended*");
        _unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _ledgerEntries.Verify(
            x => x.AddAsync(It.IsAny<LedgerEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TransferAsync_InsufficientBalance_ThrowsInsufficientBalanceException()
    {
        var source = CreateUserAccountWithId(AccountAId, 5m);
        var destination = CreateUserAccountWithId(AccountBId, 0m);

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionRecord?)null);

        _accounts
            .Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == AccountAId ? source :
                id == AccountBId ? destination : null);

        var act = () => _sut.TransferAsync(AccountAId, AccountBId, 50m, "xfer-insufficient");

        await act.Should().ThrowAsync<InsufficientBalanceException>();
        _unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _ledgerEntries.Verify(
            x => x.AddAsync(It.IsAny<LedgerEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TransferAsync_NonTransferableSource_Throws()
    {
        var source = CreateUserAccountWithId(AccountAId, 100m, isTransferable: false);
        var destination = CreateUserAccountWithId(AccountBId, 0m);

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionRecord?)null);

        _accounts
            .Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == AccountAId ? source :
                id == AccountBId ? destination : null);

        var act = () => _sut.TransferAsync(AccountAId, AccountBId, 10m, "xfer-food-budget");

        await act.Should().ThrowAsync<InvalidAccountOperationException>()
            .WithMessage("*does not allow transfers*");
    }

    private static Account CreateUserAccountWithId(
        Guid accountId,
        decimal openingBalance,
        bool isTransferable = true)
    {
        var account = TestAccounts.CreateUser(
            Guid.NewGuid(),
            "1000000002",
            isTransferable: isTransferable);
        typeof(BaseEntity)
            .GetProperty(nameof(BaseEntity.Id))!
            .SetValue(account, accountId);

        if (openingBalance > 0m)
            account.Credit(openingBalance);

        return account;
    }
}
