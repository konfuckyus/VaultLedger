using FluentAssertions;
using Moq;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

using VaultLedger.UnitTests.Helpers;

namespace VaultLedger.UnitTests.Services;

public class TopUpRequestServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ITopUpRequestRepository> _topUpRequests = new();
    private readonly Mock<ITransactionService> _transactions = new();
    private readonly TopUpRequestService _sut;

    public TopUpRequestServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _unitOfWork.SetupGet(x => x.TopUpRequests).Returns(_topUpRequests.Object);
        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new TopUpRequestService(_unitOfWork.Object, _transactions.Object);
    }

    [Fact]
    public async Task Submit_OtherUsersAccount_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateUserAccount(accountId, ownerId, 10m);

        _accounts.Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var act = () => _sut.SubmitTopUpRequestAsync(callerId, accountId, 50m);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Submit_SecondPendingForSameAccount_IsRejected()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateUserAccount(accountId, userId, 10m);

        _accounts.Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _topUpRequests
            .Setup(x => x.HasPendingByUserAndAccountAsync(userId, accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.SubmitTopUpRequestAsync(userId, accountId, 50m, "daha fazla");

        await act.Should().ThrowAsync<InvalidRequestOperationException>()
            .WithMessage("*pending top-up*");
    }

    [Fact]
    public async Task Approve_CallsTopUpAndSetsResultingTransactionId()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var request = TopUpRequest.Create(userId, accountId, 75m, hasPendingForAccount: false, "maaş");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!
            .SetValue(request, Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

        var txId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var tx = TransactionRecord.Create(
            TransactionType.TopUp,
            SystemAccounts.ClearingAccountId,
            accountId,
            75m,
            Guid.NewGuid(),
            "topup-request-approve:dddddddd-dddd-dddd-dddd-dddddddddddd",
            performedByUserId: adminId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(tx, txId);
        tx.MarkCompleted();

        _topUpRequests
            .Setup(x => x.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _transactions
            .Setup(x => x.TopUpInCurrentTransactionAsync(
                accountId,
                75m,
                It.Is<string>(k => k.Contains(request.Id.ToString("D"))),
                "maaş",
                adminId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        var result = await _sut.ApproveTopUpRequestAsync(request.Id, adminId);

        result.Status.Should().Be(RequestStatus.Approved);
        result.ResultingTransactionRecordId.Should().Be(txId);
        result.ReviewedByUserId.Should().Be(adminId);
        _unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Account CreateUserAccount(Guid accountId, Guid userId, decimal balance)
    {
        var account = TestAccounts.CreateUser(userId, "1000000077");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(account, accountId);
        if (balance > 0m) account.Credit(balance);
        return account;
    }
}
