using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.API.Extensions;
using VaultLedger.Application.DTOs.Requests;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin/account-requests")]
public sealed class AdminAccountRequestsController : ControllerBase
{
    private readonly IAccountRequestService _accountRequests;
    private readonly IUnitOfWork _unitOfWork;

    public AdminAccountRequestsController(
        IAccountRequestService accountRequests,
        IUnitOfWork unitOfWork)
    {
        _accountRequests = accountRequests;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<AccountRequestDto>>> GetPending(
        CancellationToken cancellationToken)
    {
        var items = await _accountRequests.GetPendingAccountRequestsAsync(cancellationToken);
        return Ok(items.Select(AccountRequestsController.Map).ToList());
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<AccountRequestDto>> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var request = await _accountRequests.ApproveAccountRequestAsync(
            id, User.GetUserId(), cancellationToken);
        return Ok(await MapWithUserAsync(request, cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<AccountRequestDto>> Reject(
        Guid id,
        [FromBody] RejectRequestDto body,
        CancellationToken cancellationToken)
    {
        var request = await _accountRequests.RejectAccountRequestAsync(
            id, User.GetUserId(), body.Reason, cancellationToken);
        return Ok(await MapWithUserAsync(request, cancellationToken));
    }

    private async Task<AccountRequestDto> MapWithUserAsync(
        AccountRequest request,
        CancellationToken cancellationToken)
    {
        var dto = AccountRequestsController.Map(request);
        if (string.IsNullOrEmpty(dto.UserFullName))
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            dto.UserFullName = user?.FullName ?? string.Empty;
            dto.UserEmail = user?.Email ?? string.Empty;
        }

        if (string.IsNullOrEmpty(dto.CategoryName))
        {
            var category = await _unitOfWork.BudgetCategories.GetByIdAsync(
                request.CategoryId, cancellationToken);
            dto.CategoryName = category?.Name ?? string.Empty;
        }

        return dto;
    }
}
