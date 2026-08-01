namespace VaultLedger.Application.DTOs.Cards;

public sealed class IssueCardRequestDto
{
    public Guid AccountId { get; set; }
    /// <summary>Full PAN — never stored; only hash + last four digits persist.</summary>
    public string CardNumber { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? Label { get; set; }
}
