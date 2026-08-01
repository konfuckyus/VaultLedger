using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using VaultLedger.API.Controllers;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

using VaultLedger.UnitTests.Helpers;

namespace VaultLedger.UnitTests.Services;

public class TopUpAdminTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ILedgerEntryRepository> _ledgerEntries = new();
    private readonly Mock<ITransactionRecordRepository> _transactionRecords = new();
    private readonly TransactionService _sut;

    public TopUpAdminTests()
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
    public void TopUp_Endpoint_RequiresAdminRole()
    {
        var method = typeof(TransactionsController).GetMethod(nameof(TransactionsController.TopUp));
        method.Should().NotBeNull();

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be("Admin");
    }

    [Fact]
    public async Task TopUpAsync_AsAdmin_CreditsTargetAccount_AndRecordsPerformedByUserId()
    {
        var targetAccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var adminId = Guid.NewGuid();
        var user = CreateUserAccount(targetAccountId, 10m);
        var system = Account.CreateSystemClearing(SystemAccounts.SystemUserId);
        const string key = "admin-topup-1";

        _transactionRecords
            .Setup(x => x.GetByIdempotencyKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionRecord?)null);

        _accounts
            .Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == SystemAccounts.ClearingAccountId ? system :
                id == targetAccountId ? user : null);

        TransactionRecord? saved = null;
        _transactionRecords
            .Setup(x => x.AddAsync(It.IsAny<TransactionRecord>(), It.IsAny<CancellationToken>()))
            .Callback<TransactionRecord, CancellationToken>((r, _) => saved = r)
            .Returns(Task.CompletedTask);

        var result = await _sut.TopUpAsync(targetAccountId, 40m, key, "Admin load", adminId);

        result.Type.Should().Be(TransactionType.TopUp);
        result.PerformedByUserId.Should().Be(adminId);
        result.DestinationAccountId.Should().Be(targetAccountId);
        user.Balance.Should().Be(50m);
        saved!.PerformedByUserId.Should().Be(adminId);
        _unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Account CreateUserAccount(Guid accountId, decimal balance)
    {
        var account = TestAccounts.CreateUser(Guid.NewGuid(), "1000000099");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(account, accountId);
        if (balance > 0m) account.Credit(balance);
        return account;
    }
}
