using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.Application.DTOs.Accounts;
using VaultLedger.Application.DTOs.Common;
using VaultLedger.Application.Interfaces;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin/accounts")]
public sealed class AdminAccountsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminAccountsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminAccountListItemDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _unitOfWork.Accounts.GetUserAccountsPagedAsync(
            page, pageSize, search, cancellationToken);

        return Ok(new PagedResultDto<AdminAccountListItemDto>
        {
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100),
            TotalCount = total,
            Items = items.Select(a => new AdminAccountListItemDto
            {
                Id = a.Id,
                UserId = a.UserId,
                AccountNumber = a.AccountNumber,
                OwnerFullName = a.User?.FullName ?? string.Empty,
                OwnerEmail = a.User?.Email ?? string.Empty,
                CategoryId = a.CategoryId,
                CategoryName = a.Category?.Name,
                Balance = a.Balance,
                Currency = a.Currency,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            }).ToList()
        });
    }
}
