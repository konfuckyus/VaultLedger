using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VaultLedger.API.Extensions;
using VaultLedger.API.Filters;
using VaultLedger.Application.DTOs.Transactions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.TransactionsPolicy)]
[Route("transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountOwnershipService _ownership;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SpendRequestDto> _spendValidator;
    private readonly IValidator<MoneyOperationRequestDto> _moneyValidator;
    private readonly IValidator<TransferRequestDto> _transferValidator;

    public TransactionsController(
        ITransactionService transactionService,
        IAccountOwnershipService ownership,
        IUnitOfWork unitOfWork,
        IValidator<SpendRequestDto> spendValidator,
        IValidator<MoneyOperationRequestDto> moneyValidator,
        IValidator<TransferRequestDto> transferValidator)
    {
        _transactionService = transactionService;
        _ownership = ownership;
        _unitOfWork = unitOfWork;
        _spendValidator = spendValidator;
        _moneyValidator = moneyValidator;
        _transferValidator = transferValidator;
    }

    [HttpPost("spend")]
    [RequireIdempotencyKey]
    public async Task<ActionResult<TransactionRecordDto>> Spend(
        [FromBody] SpendRequestDto request,
        CancellationToken cancellationToken)
    {
        await _spendValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _ownership.EnsureCanAccessAccountAsync(
            request.AccountId, User.GetUserId(), User.IsAdmin(), cancellationToken);

        var record = await _transactionService.SpendAsync(
            request.AccountId,
            request.CardId,
            request.Amount,
            HttpContext.GetIdempotencyKey(),
            request.Description,
            request.Pin,
            cancellationToken);

        return Ok(Map(record));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("topup")]
    [RequireIdempotencyKey]
    public async Task<ActionResult<TransactionRecordDto>> TopUp(
        [FromBody] MoneyOperationRequestDto request,
        CancellationToken cancellationToken)
    {
        await _moneyValidator.ValidateAndThrowAsync(request, cancellationToken);

        var record = await _transactionService.TopUpAsync(
            request.AccountId,
            request.Amount,
            HttpContext.GetIdempotencyKey(),
            request.Description,
            User.GetUserId(),
            cancellationToken);

        return Ok(Map(record));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("refund")]
    [RequireIdempotencyKey]
    public async Task<ActionResult<TransactionRecordDto>> Refund(
        [FromBody] MoneyOperationRequestDto request,
        CancellationToken cancellationToken)
    {
        await _moneyValidator.ValidateAndThrowAsync(request, cancellationToken);

        var record = await _transactionService.RefundAsync(
            request.AccountId,
            request.Amount,
            HttpContext.GetIdempotencyKey(),
            request.Description,
            cancellationToken);

        return Ok(Map(record));
    }

    [HttpPost("transfer")]
    [RequireIdempotencyKey]
    public async Task<ActionResult<TransactionRecordDto>> Transfer(
        [FromBody] TransferRequestDto request,
        CancellationToken cancellationToken)
    {
        await _transferValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _ownership.EnsureCanAccessAccountAsync(
            request.SourceAccountId, User.GetUserId(), User.IsAdmin(), cancellationToken);

        var record = await _transactionService.TransferAsync(
            request.SourceAccountId,
            request.DestinationAccountId,
            request.Amount,
            HttpContext.GetIdempotencyKey(),
            request.Description,
            request.Pin,
            cancellationToken);

        return Ok(Map(record));
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<TransactionRecordDto>>> History(
        [FromQuery] Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        await _ownership.EnsureCanAccessAccountAsync(
            accountId, User.GetUserId(), User.IsAdmin(), cancellationToken);

        var items = await _unitOfWork.TransactionRecords.GetHistoryForAccountAsync(
            accountId, page, pageSize, cancellationToken);

        return Ok(items.Select(Map).ToList());
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
