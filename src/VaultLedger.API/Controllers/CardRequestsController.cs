using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.API.Extensions;
using VaultLedger.Application.DTOs.Requests;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("card-requests")]
public sealed class CardRequestsController : ControllerBase
{
    private readonly ICardRequestService _cardRequests;

    public CardRequestsController(ICardRequestService cardRequests)
    {
        _cardRequests = cardRequests;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<CardRequestDto>>> GetMine(
        CancellationToken cancellationToken)
    {
        var items = await _cardRequests.GetMyCardRequestsAsync(
            User.GetUserId(), cancellationToken);
        return Ok(items.Select(Map).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<CardRequestDto>> Submit(
        [FromBody] SubmitCardRequestDto body,
        CancellationToken cancellationToken)
    {
        var request = await _cardRequests.SubmitCardRequestAsync(
            User.GetUserId(), body.AccountId, body.Label, cancellationToken);
        return CreatedAtAction(nameof(GetMine), Map(request));
    }

    internal static CardRequestDto Map(CardRequest request) => new()
    {
        Id = request.Id,
        UserId = request.UserId,
        UserFullName = request.User?.FullName ?? string.Empty,
        UserEmail = request.User?.Email ?? string.Empty,
        AccountId = request.AccountId,
        Label = request.Label,
        Status = request.Status.ToString(),
        RequestedAt = request.RequestedAt,
        ReviewedAt = request.ReviewedAt,
        ReviewedByUserId = request.ReviewedByUserId,
        RejectionReason = request.RejectionReason,
        ResultingCardId = request.ResultingCardId
    };
}
