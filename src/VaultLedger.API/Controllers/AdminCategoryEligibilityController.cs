using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.API.Extensions;
using VaultLedger.Application.DTOs.BudgetCategories;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin/category-eligibility")]
public sealed class AdminCategoryEligibilityController : ControllerBase
{
    private readonly ICategoryEligibilityService _eligibilities;
    private readonly IValidator<GrantCategoryEligibilityDto> _grantValidator;

    public AdminCategoryEligibilityController(
        ICategoryEligibilityService eligibilities,
        IValidator<GrantCategoryEligibilityDto> grantValidator)
    {
        _eligibilities = eligibilities;
        _grantValidator = grantValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryEligibilityDto>>> List(
        [FromQuery] Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (categoryId == Guid.Empty)
            return BadRequest("categoryId is required.");

        var items = await _eligibilities.ListByCategoryAsync(categoryId, cancellationToken);
        return Ok(items.Select(Map).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<CategoryEligibilityDto>> Grant(
        [FromBody] GrantCategoryEligibilityDto request,
        CancellationToken cancellationToken)
    {
        await _grantValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _eligibilities.GrantAsync(
            User.GetUserId(), request, cancellationToken);
        return Ok(Map(created));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        await _eligibilities.RevokeAsync(id, cancellationToken);
        return NoContent();
    }

    private static CategoryEligibilityDto Map(CategoryEligibility e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        UserFullName = e.User?.FullName ?? string.Empty,
        UserEmail = e.User?.Email ?? string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? string.Empty,
        GrantedByAdminUserId = e.GrantedByAdminUserId,
        GrantedAt = e.GrantedAt
    };
}
