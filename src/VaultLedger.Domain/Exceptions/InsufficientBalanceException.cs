namespace VaultLedger.Domain.Exceptions;

public sealed class InsufficientBalanceException : DomainException
{
    public InsufficientBalanceException(Guid accountId, decimal requestedAmount, decimal availableBalance)
        : base(
            "Yetersiz bakiye: hesabınızda bu işlem için yeterli tutar yok. " +
            $"(İstenen: {requestedAmount:F2}, Mevcut: {availableBalance:F2})")
    {
        AccountId = accountId;
        RequestedAmount = requestedAmount;
        AvailableBalance = availableBalance;
    }

    public Guid AccountId { get; }
    public decimal RequestedAmount { get; }
    public decimal AvailableBalance { get; }
}
