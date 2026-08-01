using FluentAssertions;
using Moq;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

using VaultLedger.UnitTests.Helpers;

namespace VaultLedger.UnitTests.Services;

public class AdjustmentTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ILedgerEntryRepository> _ledgerEntries = new();
    private readonly Mock<ITransactionRecordRepository> _transactionRecords = new();
    private readonly TransactionService _sut;

    public AdjustmentTests()
    {
        _unitOfWork.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _unitOfWork.SetupGet(x => x.LedgerEntries).Returns(_ledgerEntries.Object);
        _unitOfWork.SetupGet(x => x.TransactionRecords).Returns(_transactionRecords.Object);
        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new TransactionService(_unitOfWork.Object);
    }

    [Fact]
    public async Task Adjustment_Increase_CreditsUser_DebitsClearing()
    {
        var targetId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var adminId = Guid.NewGuid();
        var user = CreateUserAccount(targetId, 10m);
        var system = Account.CreateSystemClearing(SystemAccounts.SystemUserId);
        const string key = "adj-inc-1";

        SetupLocks(targetId, user, system, key);

        TransactionRecord? saved = null;
        _transactionRecords
            .Setup(x => x.AddAsync(It.IsAny<TransactionRecord>(), It.IsAny<CancellationToken>()))
            .Callback<TransactionRecord, CancellationToken>((r, _) => saved = r)
            .Returns(Task.CompletedTask);

        var result = await _sut.AdjustmentAsync(
            targetId, 25m, AdjustmentDirection.Increase, "Düzeltme +25", key, adminId);

        result.Type.Should().Be(TransactionType.Adjustment);
        result.PerformedByUserId.Should().Be(adminId);
        result.Description.Should().Be("Düzeltme +25");
        result.SourceAccountId.Should().Be(SystemAccounts.ClearingAccountId);
        result.DestinationAccountId.Should().Be(targetId);
        user.Balance.Should().Be(35m);
        saved!.Description.Should().Be("Düzeltme +25");
    }

    [Fact]
    public async Task Adjustment_Decrease_DebitsUser_CreditsClearing()
    {
        var targetId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var adminId = Guid.NewGuid();
        var user = CreateUserAccount(targetId, 100m);
        var system = Account.CreateSystemClearing(SystemAccounts.SystemUserId);
        const string key = "adj-dec-1";

        SetupLocks(targetId, user, system, key);

        var result = await _sut.AdjustmentAsync(
            targetId, 40m, AdjustmentDirection.Decrease, "Fazla yükleme geri al", key, adminId);

        result.Type.Should().Be(TransactionType.Adjustment);
        result.SourceAccountId.Should().Be(targetId);
        result.DestinationAccountId.Should().Be(SystemAccounts.ClearingAccountId);
        user.Balance.Should().Be(60m);
    }

    [Fact]
    public async Task Adjustment_EmptyReason_IsRejected()
    {
        var act = () => _sut.AdjustmentAsync(
            Guid.NewGuid(),
            10m,
            AdjustmentDirection.Decrease,
            "   ",
            "adj-empty",
            Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("reason");
    }

    private void SetupLocks(Guid targetId, Account user, Account system, string key)
    {
        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionRecord?)null);

        _accounts
            .Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == SystemAccounts.ClearingAccountId ? system :
                id == targetId ? user : null);

        _transactionRecords
            .Setup(x => x.AddAsync(It.IsAny<TransactionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _ledgerEntries
            .Setup(x => x.AddAsync(It.IsAny<LedgerEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static Account CreateUserAccount(Guid accountId, decimal balance)
    {
        var account = TestAccounts.CreateUser(Guid.NewGuid(), "1000000088");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(account, accountId);
        if (balance > 0m) account.Credit(balance);
        return account;
    }
}
