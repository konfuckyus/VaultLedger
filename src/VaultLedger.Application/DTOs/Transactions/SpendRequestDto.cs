namespace VaultLedger.Application.DTOs.Transactions;

public sealed class SpendRequestDto
{
    public Guid AccountId { get; set; }
    public Guid CardId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string Pin { get; set; } = string.Empty;
}
