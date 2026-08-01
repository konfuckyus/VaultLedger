using VaultLedger.Domain.Enums;

namespace VaultLedger.Application.DTOs.Transactions;

public sealed class AdjustmentRequestDto
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public AdjustmentDirection Direction { get; set; }
    public string Reason { get; set; } = string.Empty;
}
