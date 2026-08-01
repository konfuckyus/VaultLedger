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
[Route("admin/topup-requests")]
public sealed class AdminTopUpRequestsController : ControllerBase
{
    private readonly ITopUpRequestService _topUpRequests;
    private readonly IUnitOfWork _unitOfWork;

    public AdminTopUpRequestsController(
        ITopUpRequestService topUpRequests,
        IUnitOfWork unitOfWork)
    {
        _topUpRequests = topUpRequests;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TopUpRequestDto>>> GetPending(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        // Default / only supported filter for now: Pending
        _ = status;
        var items = await _topUpRequests.GetPendingTopUpRequestsAsync(cancellationToken);
        return Ok(items.Select(TopUpRequestsController.Map).ToList());
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<TopUpRequestDto>> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var request = await _topUpRequests.ApproveTopUpRequestAsync(
            id, User.GetUserId(), cancellationToken);
        return Ok(await MapWithUserAsync(request, cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<TopUpRequestDto>> Reject(
        Guid id,
        [FromBody] RejectRequestDto body,
        CancellationToken cancellationToken)
    {
        var request = await _topUpRequests.RejectTopUpRequestAsync(
            id, User.GetUserId(), body.Reason, cancellationToken);
        return Ok(await MapWithUserAsync(request, cancellationToken));
    }

    private async Task<TopUpRequestDto> MapWithUserAsync(
        TopUpRequest request,
        CancellationToken cancellationToken)
    {
        var dto = TopUpRequestsController.Map(request);
        if (!string.IsNullOrEmpty(dto.UserFullName))
            return dto;

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        dto.UserFullName = user?.FullName ?? string.Empty;
        dto.UserEmail = user?.Email ?? string.Empty;
        return dto;
    }
}
