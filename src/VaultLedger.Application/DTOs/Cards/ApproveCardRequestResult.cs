namespace VaultLedger.Application.DTOs.Cards;

/// <summary>
/// One-time card approval payload. <see cref="RawCardNumber"/> is never persisted and
/// must not appear in logs — only returned once to the approving admin.
/// </summary>
public sealed class ApproveCardRequestResult
{
    public Guid CardId { get; init; }
    public string LastFourDigits { get; init; } = string.Empty;
    public string MaskedNumber { get; init; } = string.Empty;

    /// <summary>Plaintext PAN — only in this response; never stored.</summary>
    public string RawCardNumber { get; init; } = string.Empty;

    public string? Label { get; init; }
}
