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
[Route("topup-requests")]
public sealed class TopUpRequestsController : ControllerBase
{
    private readonly ITopUpRequestService _topUpRequests;
    private readonly IValidator<SubmitTopUpRequestDto> _submitValidator;

    public TopUpRequestsController(
        ITopUpRequestService topUpRequests,
        IValidator<SubmitTopUpRequestDto> submitValidator)
    {
        _topUpRequests = topUpRequests;
        _submitValidator = submitValidator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<TopUpRequestDto>>> GetMine(
        CancellationToken cancellationToken)
    {
        var items = await _topUpRequests.GetMyTopUpRequestsAsync(
            User.GetUserId(), cancellationToken);
        return Ok(items.Select(Map).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<TopUpRequestDto>> Submit(
        [FromBody] SubmitTopUpRequestDto body,
        CancellationToken cancellationToken)
    {
        await _submitValidator.ValidateAndThrowAsync(body, cancellationToken);
        var request = await _topUpRequests.SubmitTopUpRequestAsync(
            User.GetUserId(), body.AccountId, body.Amount, body.Note, cancellationToken);
        return CreatedAtAction(nameof(GetMine), Map(request));
    }

    internal static TopUpRequestDto Map(TopUpRequest request) => new()
    {
        Id = request.Id,
        UserId = request.UserId,
        UserFullName = request.User?.FullName ?? string.Empty,
        UserEmail = request.User?.Email ?? string.Empty,
        AccountId = request.AccountId,
        Amount = request.Amount,
        Note = request.Note,
        Status = request.Status.ToString(),
        RequestedAt = request.RequestedAt,
        ReviewedAt = request.ReviewedAt,
        ReviewedByUserId = request.ReviewedByUserId,
        RejectionReason = request.RejectionReason,
        ResultingTransactionRecordId = request.ResultingTransactionRecordId
    };
}
