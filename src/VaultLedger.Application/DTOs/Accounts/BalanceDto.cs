namespace VaultLedger.Application.DTOs.Accounts;

public sealed class BalanceDto
{
    public Guid AccountId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
}
