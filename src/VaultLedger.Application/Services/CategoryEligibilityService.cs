using VaultLedger.Application.DTOs.BudgetCategories;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Services;

public sealed class CategoryEligibilityService : ICategoryEligibilityService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryEligibilityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryEligibility> GrantAsync(
        Guid adminUserId,
        GrantCategoryEligibilityDto request,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user id is required.", nameof(adminUserId));

        _ = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        _ = await _unitOfWork.BudgetCategories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(BudgetCategoryDefinition), request.CategoryId);

        var existing = await _unitOfWork.CategoryEligibilities.GetByUserAndCategoryAsync(
            request.UserId, request.CategoryId, cancellationToken);
        if (existing is not null)
            return existing;

        var eligibility = CategoryEligibility.Create(
            request.UserId, request.CategoryId, adminUserId);
        await _unitOfWork.CategoryEligibilities.AddAsync(eligibility, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _unitOfWork.CategoryEligibilities.GetByUserAndCategoryAsync(
            request.UserId, request.CategoryId, cancellationToken)
            ?? eligibility;
    }

    public async Task RevokeAsync(Guid eligibilityId, CancellationToken cancellationToken = default)
    {
        var eligibility = await _unitOfWork.CategoryEligibilities.GetByIdAsync(
            eligibilityId, cancellationToken)
            ?? throw new NotFoundException(nameof(CategoryEligibility), eligibilityId);

        _unitOfWork.CategoryEligibilities.Remove(eligibility);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<CategoryEligibility>> ListByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
        => _unitOfWork.CategoryEligibilities.GetByCategoryIdAsync(categoryId, cancellationToken);

    public async Task<IReadOnlyList<BudgetCategoryDefinition>> GetAvailableToUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var eligibleIds = await _unitOfWork.CategoryEligibilities.GetCategoryIdsForUserAsync(
            userId, cancellationToken);
        var eligibleSet = eligibleIds.ToHashSet();

        var all = await _unitOfWork.BudgetCategories.GetAllAsync(cancellationToken);
        return all
            .Where(c => c.IsActive && (c.IsSelfRequestable || eligibleSet.Contains(c.Id)))
            .OrderByDescending(c => c.IsSystemDefault)
            .ThenBy(c => c.Name)
            .ToList();
    }
}
