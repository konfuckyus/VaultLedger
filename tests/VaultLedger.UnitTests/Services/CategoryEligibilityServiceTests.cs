using FluentAssertions;
using Moq;
using VaultLedger.Application.DTOs.BudgetCategories;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Application.Services;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.UnitTests.Helpers;

namespace VaultLedger.UnitTests.Services;

public class CategoryEligibilityServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBudgetCategoryRepository> _categories = new();
    private readonly Mock<ICategoryEligibilityRepository> _eligibilities = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IAccountRequestRepository> _accountRequests = new();
    private readonly Mock<IAccountNumberGenerator> _numbers = new();
    private readonly Mock<ICardNumberGenerator> _cards = new();
    private readonly Mock<ITransactionService> _transactions = new();
    private readonly CategoryEligibilityService _eligibilitySut;
    private readonly AccountRequestService _requestSut;

    public CategoryEligibilityServiceTests()
    {
        _uow.SetupGet(x => x.BudgetCategories).Returns(_categories.Object);
        _uow.SetupGet(x => x.CategoryEligibilities).Returns(_eligibilities.Object);
        _uow.SetupGet(x => x.Users).Returns(_users.Object);
        _uow.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _uow.SetupGet(x => x.AccountRequests).Returns(_accountRequests.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _eligibilitySut = new CategoryEligibilityService(_uow.Object);
        _requestSut = new AccountRequestService(
            _uow.Object,
            _numbers.Object,
            _cards.Object,
            _transactions.Object);
    }

    [Fact]
    public async Task Submit_RestrictedCategory_WithoutEligibility_ThrowsForbidden()
    {
        var userId = Guid.NewGuid();
        var category = RestrictedCategory();
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _eligibilities.Setup(x => x.ExistsAsync(userId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _requestSut.SubmitAccountRequestAsync(userId, category.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Submit_RestrictedCategory_WithEligibility_Succeeds()
    {
        var userId = Guid.NewGuid();
        var category = RestrictedCategory();
        _categories.Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _eligibilities.Setup(x => x.ExistsAsync(userId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _accounts.Setup(x => x.ExistsByUserAndCategoryAsync(userId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accountRequests.Setup(x => x.HasPendingByUserAndCategoryAsync(
                userId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accountRequests.Setup(x => x.AddAsync(It.IsAny<AccountRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _requestSut.SubmitAccountRequestAsync(userId, category.Id);

        result.CategoryId.Should().Be(category.Id);
        result.Status.Should().Be(RequestStatus.Pending);
    }

    [Fact]
    public async Task GetAvailableToUser_IncludesSelfRequestableAndGranted()
    {
        var userId = Guid.NewGuid();
        var open = BudgetCategoryDefinition.Create("Genel", 0m, true, isSelfRequestable: true);
        var restricted = BudgetCategoryDefinition.Create("VIP", 0m, true, isSelfRequestable: false);
        var other = BudgetCategoryDefinition.Create("Gizli", 0m, true, isSelfRequestable: false);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(restricted, Guid.NewGuid());
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(other, Guid.NewGuid());

        _categories.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([open, restricted, other]);
        _eligibilities.Setup(x => x.GetCategoryIdsForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([restricted.Id]);

        var result = await _eligibilitySut.GetAvailableToUserAsync(userId);

        result.Should().Contain(c => c.Id == open.Id);
        result.Should().Contain(c => c.Id == restricted.Id);
        result.Should().NotContain(c => c.Id == other.Id);
    }

    [Fact]
    public async Task GrantAsync_Existing_IsIdempotent()
    {
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var existing = CategoryEligibility.Create(userId, categoryId, adminId);

        _users.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create("U", "u@t.com", "h", UserRole.User));
        _categories.Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BudgetCategoryDefinition.Create("X", 0m, true, false));
        _eligibilities.Setup(x => x.GetByUserAndCategoryAsync(userId, categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _eligibilitySut.GrantAsync(
            adminId, new GrantCategoryEligibilityDto { UserId = userId, CategoryId = categoryId });

        result.Should().BeSameAs(existing);
        _eligibilities.Verify(
            x => x.AddAsync(It.IsAny<CategoryEligibility>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static BudgetCategoryDefinition RestrictedCategory()
    {
        var category = BudgetCategoryDefinition.Create(
            "Kurumsal", 0m, true, isSelfRequestable: false);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!
            .SetValue(category, Guid.NewGuid());
        return category;
    }
}
