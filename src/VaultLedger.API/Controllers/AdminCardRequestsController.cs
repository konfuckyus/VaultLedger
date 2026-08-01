using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.API.Extensions;
using VaultLedger.Application.DTOs.Cards;
using VaultLedger.Application.DTOs.Requests;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin/card-requests")]
public sealed class AdminCardRequestsController : ControllerBase
{
    private readonly ICardRequestService _cardRequests;
    private readonly IUnitOfWork _unitOfWork;

    public AdminCardRequestsController(
        ICardRequestService cardRequests,
        IUnitOfWork unitOfWork)
    {
        _cardRequests = cardRequests;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<CardRequestDto>>> GetPending(
        CancellationToken cancellationToken)
    {
        var items = await _cardRequests.GetPendingCardRequestsAsync(cancellationToken);
        return Ok(items.Select(CardRequestsController.Map).ToList());
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApproveCardRequestResult>> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        // RawCardNumber is returned once in this body only — never logged or stored.
        var result = await _cardRequests.ApproveCardRequestAsync(
            id, User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<CardRequestDto>> Reject(
        Guid id,
        [FromBody] RejectRequestDto body,
        CancellationToken cancellationToken)
    {
        var request = await _cardRequests.RejectCardRequestAsync(
            id, User.GetUserId(), body.Reason, cancellationToken);
        var dto = CardRequestsController.Map(request);
        if (string.IsNullOrEmpty(dto.UserFullName))
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            dto.UserFullName = user?.FullName ?? string.Empty;
            dto.UserEmail = user?.Email ?? string.Empty;
        }

        return Ok(dto);
    }
}
