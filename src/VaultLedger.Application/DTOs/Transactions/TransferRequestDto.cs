namespace VaultLedger.Application.DTOs.Transactions;

public sealed class TransferRequestDto
{
    public Guid SourceAccountId { get; set; }
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string Pin { get; set; } = string.Empty;
}
