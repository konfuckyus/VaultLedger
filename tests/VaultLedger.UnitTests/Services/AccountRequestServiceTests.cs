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

public class AccountRequestServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IAccountRequestRepository> _accountRequests = new();
    private readonly Mock<IBudgetCategoryRepository> _categories = new();
    private readonly Mock<ICardRepository> _cards = new();
    private readonly Mock<IAccountNumberGenerator> _accountNumberGenerator = new();
    private readonly Mock<ICardNumberGenerator> _cardNumberGenerator = new();
    private readonly Mock<ITransactionService> _transactions = new();
    private readonly AccountRequestService _sut;

    public AccountRequestServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _unitOfWork.SetupGet(x => x.AccountRequests).Returns(_accountRequests.Object);
        _unitOfWork.SetupGet(x => x.BudgetCategories).Returns(_categories.Object);
        _unitOfWork.SetupGet(x => x.Cards).Returns(_cards.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _cardNumberGenerator.Setup(x => x.GenerateUniqueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedCardNumber("hash", "4242", "4111111111114242"));

        _sut = new AccountRequestService(
            _unitOfWork.Object,
            _accountNumberGenerator.Object,
            _cardNumberGenerator.Object,
            _transactions.Object);
    }

    [Fact]
    public async Task SubmitAccountRequestAsync_UserAlreadyHasCategoryAccount_Throws()
    {
        var userId = Guid.NewGuid();
        var category = ActiveCategory(SystemBudgetCategories.YemekId, "Yemek", 250m, transferable: false);
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _accounts.Setup(x => x.ExistsByUserAndCategoryAsync(userId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.SubmitAccountRequestAsync(userId, category.Id);

        await act.Should().ThrowAsync<InvalidRequestOperationException>()
            .WithMessage("*already has an account for this category*");
        _accountRequests.Verify(
            x => x.AddAsync(It.IsAny<AccountRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubmitAccountRequestAsync_PendingForSameCategory_Throws()
    {
        var userId = Guid.NewGuid();
        var category = ActiveCategory(SystemBudgetCategories.GenelId, "Genel", 0m, transferable: true);
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _accounts.Setup(x => x.ExistsByUserAndCategoryAsync(userId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accountRequests.Setup(x => x.HasPendingByUserAndCategoryAsync(
                userId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.SubmitAccountRequestAsync(userId, category.Id);

        await act.Should().ThrowAsync<InvalidRequestOperationException>()
            .WithMessage("*pending account request*");
        _accountRequests.Verify(
            x => x.AddAsync(It.IsAny<AccountRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveAccountRequestAsync_CreatesAccount_AndAppliesCategoryTopUp()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var category = ActiveCategory(SystemBudgetCategories.YemekId, "Yemek", 250m, transferable: false);
        var request = AccountRequest.Create(userId, category.Id, hasPendingForCategory: false);
        var generatedNumber = "5555666677";

        _accountRequests.Setup(x => x.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _accounts.Setup(x => x.ExistsByUserAndCategoryAsync(userId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accountNumberGenerator.Setup(x => x.GenerateUniqueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedNumber);

        Account? createdAccount = null;
        _accounts.Setup(x => x.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((account, _) => createdAccount = account)
            .Returns(Task.CompletedTask);

        _transactions.Setup(x => x.TopUpInCurrentTransactionAsync(
                It.IsAny<Guid>(),
                250m,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                adminId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid targetId, decimal amount, string key, string? desc, Guid admin, CancellationToken _) =>
                TransactionRecord.Create(
                    TransactionType.TopUp,
                    SystemAccounts.ClearingAccountId,
                    targetId,
                    amount,
                    Guid.NewGuid(),
                    key,
                    performedByUserId: admin,
                    description: desc));

        var result = await _sut.ApproveAccountRequestAsync(request.Id, adminId);

        result.Status.Should().Be(RequestStatus.Approved);
        result.ReviewedByUserId.Should().Be(adminId);
        result.ResultingAccountId.Should().Be(createdAccount!.Id);
        createdAccount.AccountNumber.Should().Be(generatedNumber);
        createdAccount.UserId.Should().Be(userId);
        createdAccount.CategoryId.Should().Be(category.Id);
        createdAccount.IsTransferable.Should().BeFalse();
        createdAccount.Status.Should().Be(AccountStatus.Active);

        _transactions.Verify(x => x.TopUpInCurrentTransactionAsync(
            createdAccount.Id,
            250m,
            $"account-request-approve:{request.Id:D}",
            "Otomatik kategori bakiyesi: Yemek",
            adminId,
            It.IsAny<CancellationToken>()), Times.Once);
        _cards.Verify(x => x.AddAsync(
            It.Is<Card>(c => c.AccountId == createdAccount.Id && c.Label == "Yemek"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAccountRequestAsync_NumberGenerationFails_DoesNotApprove_AndRollsBack()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var category = ActiveCategory(SystemBudgetCategories.GenelId, "Genel", 0m, transferable: true);
        var request = AccountRequest.Create(userId, category.Id, hasPendingForCategory: false);

        _accountRequests.Setup(x => x.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _accounts.Setup(x => x.ExistsByUserAndCategoryAsync(userId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accountNumberGenerator.Setup(x => x.GenerateUniqueAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccountNumberGenerationException("collision"));

        var act = () => _sut.ApproveAccountRequestAsync(request.Id, adminId);

        await act.Should().ThrowAsync<AccountNumberGenerationException>();
        request.Status.Should().Be(RequestStatus.Pending);
        request.ResultingAccountId.Should().BeNull();
        _accounts.Verify(x => x.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        _cards.Verify(x => x.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()), Times.Never);
        _transactions.Verify(x => x.TopUpInCurrentTransactionAsync(
            It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void SystemDefaultCategory_CannotRenameOrDeactivate()
    {
        var genel = BudgetCategoryDefinition.Create(
            "Genel", 0m, true, isSelfRequestable: true, isSystemDefault: true);

        var rename = () => genel.Rename("Other");
        rename.Should().Throw<InvalidAccountOperationException>().WithMessage("*cannot be changed*");

        var deactivate = () => genel.Deactivate();
        deactivate.Should().Throw<InvalidAccountOperationException>().WithMessage("*cannot be deactivated*");
    }

    private static BudgetCategoryDefinition ActiveCategory(
        Guid id,
        string name,
        decimal amount,
        bool transferable)
    {
        var category = BudgetCategoryDefinition.Create(name, amount, transferable);
        typeof(BaseEntity)
            .GetProperty(nameof(BaseEntity.Id))!
            .SetValue(category, id);
        return category;
    }
}
