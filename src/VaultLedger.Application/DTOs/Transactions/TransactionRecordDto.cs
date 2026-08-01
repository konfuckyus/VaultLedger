namespace VaultLedger.Application.DTOs.Transactions;

public sealed class TransactionRecordDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid SourceAccountId { get; set; }
    public Guid? DestinationAccountId { get; set; }
    public Guid? CardId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid TransactionGroupId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
