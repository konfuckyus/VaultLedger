namespace VaultLedger.Domain.Enums;

public enum TransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    RolledBack = 3
}
