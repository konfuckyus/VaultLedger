using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.API.Extensions;
using VaultLedger.Application.DTOs.BudgetCategories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("budget-categories")]
public sealed class BudgetCategoriesController : ControllerBase
{
    private readonly IBudgetCategoryService _categories;
    private readonly ICategoryEligibilityService _eligibilities;

    public BudgetCategoriesController(
        IBudgetCategoryService categories,
        ICategoryEligibilityService eligibilities)
    {
        _categories = categories;
        _eligibilities = eligibilities;
    }

    /// <summary>Self-requestable active categories only (legacy).</summary>
    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<BudgetCategoryDto>>> GetActive(
        CancellationToken cancellationToken)
    {
        var items = await _categories.GetActiveAsync(cancellationToken);
        return Ok(items.Select(Map).ToList());
    }

    /// <summary>
    /// Categories the current user may request: self-requestable + eligibility grants.
    /// </summary>
    [HttpGet("available-to-me")]
    public async Task<ActionResult<IReadOnlyList<BudgetCategoryDto>>> GetAvailableToMe(
        CancellationToken cancellationToken)
    {
        var items = await _eligibilities.GetAvailableToUserAsync(
            User.GetUserId(), cancellationToken);
        return Ok(items.Select(Map).ToList());
    }

    private static BudgetCategoryDto Map(BudgetCategoryDefinition category) => new()
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
