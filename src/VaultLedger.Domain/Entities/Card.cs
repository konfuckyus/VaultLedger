using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Domain.Entities;

/// <summary>
/// Card linked to an account. Full PAN is never stored — only a one-way hash and last four digits.
/// An account may hold multiple active cards (e.g. "Yemek", "Kurumsal").
/// </summary>
public class Card : BaseEntity
{
    public const int MaxLabelLength = 64;

    private Card()
    {
    }

    private Card(
        Guid accountId,
        string cardNumberHash,
        string lastFourDigits,
        DateTime expiresAt,
        string? label)
    {
        AccountId = accountId;
        CardNumberHash = cardNumberHash;
        LastFourDigits = lastFourDigits;
        Label = NormalizeLabel(label);
        Status = CardStatus.Active;
        IssuedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = null!;

    /// <summary>One-way hash of the full card number. Never store plaintext PAN.</summary>
    public string CardNumberHash { get; private set; } = null!;

    /// <summary>Last four digits for display (e.g. masked form ****1234).</summary>
    public string LastFourDigits { get; private set; } = null!;

    /// <summary>Optional display label (e.g. "Yemek", "Kurumsal").</summary>
    public string? Label { get; private set; }

    public CardStatus Status { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public string MaskedNumber => $"****{LastFourDigits}";

    public static Card Issue(
        Guid accountId,
        string cardNumberHash,
        string lastFourDigits,
        DateTime expiresAt,
        string? label = null)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is required.", nameof(accountId));

        ArgumentException.ThrowIfNullOrWhiteSpace(cardNumberHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastFourDigits);

        if (lastFourDigits.Length != 4 || !lastFourDigits.All(char.IsDigit))
            throw new ArgumentException("LastFourDigits must be exactly 4 digits.", nameof(lastFourDigits));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("ExpiresAt must be in the future.", nameof(expiresAt));

        return new Card(accountId, cardNumberHash, lastFourDigits, expiresAt, label);
    }

    public void Block()
    {
        if (Status == CardStatus.Expired)
            throw new InvalidAccountOperationException("An expired card cannot be blocked.");

        Status = CardStatus.Blocked;
    }

    public void Unblock()
    {
        if (Status == CardStatus.Expired)
            throw new InvalidAccountOperationException("An expired card cannot be unblocked.");

        if (ExpiresAt <= DateTime.UtcNow)
        {
            Status = CardStatus.Expired;
            throw new InvalidAccountOperationException("Card has expired and cannot be activated.");
        }

        Status = CardStatus.Active;
    }

    public void MarkExpired() => Status = CardStatus.Expired;

    private static string? NormalizeLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return null;

        var trimmed = label.Trim();
        if (trimmed.Length > MaxLabelLength)
        {
            throw new ArgumentException(
                $"Label must be at most {MaxLabelLength} characters.",
                nameof(label));
        }

        return trimmed;
    }
}
