using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VaultLedger.API.Extensions;
using VaultLedger.API.Filters;
using VaultLedger.Application.DTOs.Transactions;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitingExtensions.TransactionsPolicy)]
[Route("admin/transactions")]
public sealed class AdminTransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IValidator<AdjustmentRequestDto> _adjustmentValidator;

    public AdminTransactionsController(
        ITransactionService transactionService,
        IValidator<AdjustmentRequestDto> adjustmentValidator)
    {
        _transactionService = transactionService;
        _adjustmentValidator = adjustmentValidator;
    }

    [HttpPost("adjustment")]
    [RequireIdempotencyKey]
    public async Task<ActionResult<TransactionRecordDto>> Adjustment(
        [FromBody] AdjustmentRequestDto request,
        CancellationToken cancellationToken)
    {
        await _adjustmentValidator.ValidateAndThrowAsync(request, cancellationToken);

        var record = await _transactionService.AdjustmentAsync(
            request.AccountId,
            request.Amount,
            request.Direction,
            request.Reason,
            HttpContext.GetIdempotencyKey(),
            User.GetUserId(),
            cancellationToken);

        return Ok(Map(record));
    }

    private static TransactionRecordDto Map(TransactionRecord record) => new()
    {
        Id = record.Id,
        Type = record.Type.ToString(),
        SourceAccountId = record.SourceAccountId,
        DestinationAccountId = record.DestinationAccountId,
        CardId = record.CardId,
        PerformedByUserId = record.PerformedByUserId,
        Amount = record.Amount,
        Status = record.Status.ToString(),
        TransactionGroupId = record.TransactionGroupId,
        IdempotencyKey = record.IdempotencyKey,
        Description = record.Description,
        CreatedAt = record.CreatedAt
    };
}
