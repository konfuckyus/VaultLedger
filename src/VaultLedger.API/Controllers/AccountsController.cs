using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.API.Extensions;
using VaultLedger.Application.DTOs.Accounts;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountOwnershipService _ownership;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly IValidator<CreateAccountRequestDto> _createValidator;

    public AccountsController(
        IUnitOfWork unitOfWork,
        IAccountOwnershipService ownership,
        IAccountNumberGenerator accountNumberGenerator,
        IValidator<CreateAccountRequestDto> createValidator)
    {
        _unitOfWork = unitOfWork;
        _ownership = ownership;
        _accountNumberGenerator = accountNumberGenerator;
        _createValidator = createValidator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetMyAccounts(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var accounts = await _unitOfWork.Accounts.GetByUserIdAsync(userId, cancellationToken);
        return Ok(accounts.Select(a => Map(a)).ToList());
    }

    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<BalanceDto>> GetBalance(Guid id, CancellationToken cancellationToken)
    {
        var account = await _ownership.EnsureCanAccessAccountAsync(
            id, User.GetUserId(), User.IsAdmin(), cancellationToken);

        return Ok(new BalanceDto
        {
            AccountId = account.Id,
            Balance = account.Balance,
            Currency = account.Currency
        });
    }

    /// <summary>
    /// Limited peer lookup for transfers — no balance. Any authenticated user.
    /// </summary>
    [HttpGet("lookup/{accountNumber}")]
    public async Task<ActionResult<AccountLookupDto>> LookupByAccountNumber(
        string accountNumber,
        CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByAccountNumberAsync(accountNumber, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException(nameof(Account), accountNumber);

        var owner = await _unitOfWork.Users.GetByIdAsync(account.UserId, cancellationToken);
        var displayName = MaskDisplayName(owner?.FullName);

        return Ok(new AccountLookupDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            Status = account.Status.ToString(),
            OwnerDisplayName = displayName
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("by-number/{accountNumber}")]
    public async Task<ActionResult<AccountDto>> GetByAccountNumber(
        string accountNumber,
        CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByAccountNumberAsync(accountNumber, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException(nameof(Account), accountNumber);

        var owner = await _unitOfWork.Users.GetByIdAsync(account.UserId, cancellationToken);
        return Ok(Map(account, owner));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(
        [FromBody] CreateAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException(nameof(User), request.UserId);

        var categoryId = request.CategoryId ?? Domain.Common.SystemBudgetCategories.GenelId;
        var category = await _unitOfWork.BudgetCategories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException(nameof(BudgetCategoryDefinition), categoryId);

        if (!category.IsActive)
            throw new Domain.Exceptions.InvalidAccountOperationException("Selected budget category is not active.");

        if (await _unitOfWork.Accounts.ExistsByUserAndCategoryAsync(user.Id, category.Id, cancellationToken))
        {
            throw new Domain.Exceptions.InvalidAccountOperationException(
                "User already has an account for this category.");
        }

        var accountNumber = await _accountNumberGenerator.GenerateUniqueAsync(cancellationToken);
        var account = Account.Create(
            user.Id,
            accountNumber,
            category.Id,
            category.IsTransferable,
            request.Currency ?? Account.DefaultCurrency);
        await _unitOfWork.Accounts.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetBalance), new { id = account.Id }, Map(account, user));
    }

    private static AccountDto Map(Account account, User? owner = null) => new()
    {
        Id = account.Id,
        UserId = account.UserId,
        AccountNumber = account.AccountNumber,
        Balance = account.Balance,
        Currency = account.Currency,
        AccountType = account.AccountType.ToString(),
        Status = account.Status.ToString(),
        CreatedAt = account.CreatedAt,
        CategoryId = account.CategoryId,
        CategoryName = account.Category?.Name,
        IsTransferable = account.IsTransferable,
        OwnerFullName = owner?.FullName,
        OwnerEmail = owner?.Email
    };

    private static string MaskDisplayName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "VaultLedger kullanıcı";

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0];

        var lastInitial = parts[^1][0];
        return $"{parts[0]} {char.ToUpperInvariant(lastInitial)}.";
    }
}
