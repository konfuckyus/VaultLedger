using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.Application.DTOs.BudgetCategories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin/budget-categories")]
public sealed class AdminBudgetCategoriesController : ControllerBase
{
    private readonly IBudgetCategoryService _categories;
    private readonly IValidator<CreateBudgetCategoryDto> _createValidator;
    private readonly IValidator<UpdateBudgetCategoryDto> _updateValidator;

    public AdminBudgetCategoriesController(
        IBudgetCategoryService categories,
        IValidator<CreateBudgetCategoryDto> createValidator,
        IValidator<UpdateBudgetCategoryDto> updateValidator)
    {
        _categories = categories;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BudgetCategoryDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var items = await _categories.GetAllAsync(cancellationToken);
        return Ok(items.Select(Map).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<BudgetCategoryDto>> Create(
        [FromBody] CreateBudgetCategoryDto request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _categories.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), Map(created));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BudgetCategoryDto>> Update(
        Guid id,
        [FromBody] UpdateBudgetCategoryDto request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var updated = await _categories.UpdateAsync(id, request, cancellationToken);
        return Ok(Map(updated));
    }

    internal static BudgetCategoryDto Map(BudgetCategoryDefinition category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        DefaultAllocatedAmount = category.DefaultAllocatedAmount,
        IsTransferable = category.IsTransferable,
        IsSelfRequestable = category.IsSelfRequestable,
        IsActive = category.IsActive,
        IsSystemDefault = category.IsSystemDefault,
        CreatedAt = category.CreatedAt
    };
}
