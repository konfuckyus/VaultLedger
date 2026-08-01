using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.API.Extensions;
using VaultLedger.Application.DTOs.Requests;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("account-requests")]
public sealed class AccountRequestsController : ControllerBase
{
    private readonly IAccountRequestService _accountRequests;
    private readonly IValidator<SubmitAccountRequestDto> _submitValidator;

    public AccountRequestsController(
        IAccountRequestService accountRequests,
        IValidator<SubmitAccountRequestDto> submitValidator)
    {
        _accountRequests = accountRequests;
        _submitValidator = submitValidator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<AccountRequestDto>>> GetMine(
        CancellationToken cancellationToken)
    {
        var items = await _accountRequests.GetMyAccountRequestsAsync(
            User.GetUserId(), cancellationToken);
        return Ok(items.Select(Map).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<AccountRequestDto>> Submit(
        [FromBody] SubmitAccountRequestDto body,
        CancellationToken cancellationToken)
    {
        await _submitValidator.ValidateAndThrowAsync(body, cancellationToken);
        var request = await _accountRequests.SubmitAccountRequestAsync(
            User.GetUserId(), body.CategoryId, cancellationToken);
        return CreatedAtAction(nameof(GetMine), Map(request));
    }

    internal static AccountRequestDto Map(AccountRequest request) => new()
    {
        Id = request.Id,
        UserId = request.UserId,
        UserFullName = request.User?.FullName ?? string.Empty,
        UserEmail = request.User?.Email ?? string.Empty,
        CategoryId = request.CategoryId,
        CategoryName = request.Category?.Name ?? string.Empty,
        Status = request.Status.ToString(),
        RequestedAt = request.RequestedAt,
        ReviewedAt = request.ReviewedAt,
        ReviewedByUserId = request.ReviewedByUserId,
        RejectionReason = request.RejectionReason,
        ResultingAccountId = request.ResultingAccountId
    };
}
