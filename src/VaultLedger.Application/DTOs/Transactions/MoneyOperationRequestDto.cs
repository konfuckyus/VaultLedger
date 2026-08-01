namespace VaultLedger.Application.DTOs.Transactions;

public sealed class MoneyOperationRequestDto
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}
