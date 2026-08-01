namespace VaultLedger.Application.DTOs.Cards;

public sealed class CardDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string? Label { get; set; }
    public string MaskedNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
