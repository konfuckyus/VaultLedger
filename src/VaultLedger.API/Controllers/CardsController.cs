using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.API.Extensions;
using VaultLedger.Application.DTOs.Cards;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("cards")]
public sealed class CardsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountOwnershipService _ownership;
    private readonly ICardNumberHasher _cardNumberHasher;
    private readonly IValidator<IssueCardRequestDto> _issueValidator;

    public CardsController(
        IUnitOfWork unitOfWork,
        IAccountOwnershipService ownership,
        ICardNumberHasher cardNumberHasher,
        IValidator<IssueCardRequestDto> issueValidator)
    {
        _unitOfWork = unitOfWork;
        _ownership = ownership;
        _cardNumberHasher = cardNumberHasher;
        _issueValidator = issueValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CardDto>>> ListByAccount(
        [FromQuery] Guid accountId,
        CancellationToken cancellationToken)
    {
        await _ownership.EnsureCanAccessAccountAsync(
            accountId, User.GetUserId(), User.IsAdmin(), cancellationToken);

        var cards = await _unitOfWork.Cards.GetByAccountIdAsync(accountId, cancellationToken);
        return Ok(cards.Select(Map).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<CardDto>> Issue(
        [FromBody] IssueCardRequestDto request,
        CancellationToken cancellationToken)
    {
        await _issueValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _ownership.EnsureCanAccessAccountAsync(
            request.AccountId, User.GetUserId(), User.IsAdmin(), cancellationToken);

        var hash = _cardNumberHasher.Hash(request.CardNumber);
        var existing = await _unitOfWork.Cards.GetByCardNumberHashAsync(hash, cancellationToken);
        if (existing is not null)
            throw new InvalidAccountOperationException("Card number is already registered.");

        var card = Card.Issue(
            request.AccountId,
            hash,
            _cardNumberHasher.LastFour(request.CardNumber),
            request.ExpiresAt,
            request.Label);

        await _unitOfWork.Cards.AddAsync(card, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Issue), new { id = card.Id }, Map(card));
    }

    [HttpPatch("{id:guid}/block")]
    public async Task<ActionResult<CardDto>> Block(Guid id, CancellationToken cancellationToken)
    {
        var card = await GetOwnedCardAsync(id, cancellationToken);
        card.Block();
        _unitOfWork.Cards.Update(card);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(Map(card));
    }

    [HttpPatch("{id:guid}/unblock")]
    public async Task<ActionResult<CardDto>> Unblock(Guid id, CancellationToken cancellationToken)
    {
        var card = await GetOwnedCardAsync(id, cancellationToken);
        card.Unblock();
        _unitOfWork.Cards.Update(card);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(Map(card));
    }

    private async Task<Card> GetOwnedCardAsync(Guid cardId, CancellationToken cancellationToken)
    {
        var card = await _unitOfWork.Cards.GetByIdAsync(cardId, cancellationToken)
            ?? throw new NotFoundException(nameof(Card), cardId);

        await _ownership.EnsureCanAccessAccountAsync(
            card.AccountId, User.GetUserId(), User.IsAdmin(), cancellationToken);

        return card;
    }

    private static CardDto Map(Card card) => new()
    {
        Id = card.Id,
        AccountId = card.AccountId,
        Label = card.Label,
        MaskedNumber = card.MaskedNumber,
        Status = card.Status.ToString(),
        IssuedAt = card.IssuedAt,
        ExpiresAt = card.ExpiresAt
    };
}
