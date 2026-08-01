namespace VaultLedger.Domain.Enums;

public enum TransactionType
{
    Spend = 0,
    Transfer = 1,
    TopUp = 2,
    Refund = 3,
    Adjustment = 4
}
