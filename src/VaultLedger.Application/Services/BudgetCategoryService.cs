using VaultLedger.Application.DTOs.BudgetCategories;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Application.Services;

public sealed class BudgetCategoryService : IBudgetCategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public BudgetCategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<BudgetCategoryDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => _unitOfWork.BudgetCategories.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<BudgetCategoryDefinition>> GetActiveAsync(
        CancellationToken cancellationToken = default)
        => _unitOfWork.BudgetCategories.GetActiveAsync(cancellationToken);

    public async Task<BudgetCategoryDefinition> CreateAsync(
        CreateBudgetCategoryDto request,
        CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.BudgetCategories.ExistsByNameAsync(request.Name, null, cancellationToken))
        {
            throw new InvalidAccountOperationException(
                $"A budget category named '{request.Name.Trim()}' already exists.");
        }

        var category = BudgetCategoryDefinition.Create(
            request.Name,
            request.DefaultAllocatedAmount,
            request.IsTransferable,
            request.IsSelfRequestable);

        await _unitOfWork.BudgetCategories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<BudgetCategoryDefinition> UpdateAsync(
        Guid id,
        UpdateBudgetCategoryDto request,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.BudgetCategories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(BudgetCategoryDefinition), id);

        var nextAmount = request.DefaultAllocatedAmount ?? category.DefaultAllocatedAmount;
        var nextTransferable = request.IsTransferable ?? category.IsTransferable;

        if (request.DefaultAllocatedAmount.HasValue || request.IsTransferable.HasValue)
            category.UpdateDefaults(nextAmount, nextTransferable);

        if (request.IsSelfRequestable.HasValue)
            category.SetSelfRequestable(request.IsSelfRequestable.Value);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                category.Activate();
            else
                category.Deactivate();
        }

        _unitOfWork.BudgetCategories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return category;
    }
}
