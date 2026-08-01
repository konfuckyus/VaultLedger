namespace VaultLedger.Application.Exceptions;

/// <summary>
/// Thrown when an Idempotency-Key maps to a Pending transaction (in-flight / incomplete).
/// </summary>
public sealed class IdempotencyInProgressException : AppException
{
    public IdempotencyInProgressException(string idempotencyKey)
        : base($"Transaction with idempotency key '{idempotencyKey}' is still being processed. Please retry later.")
    {
        IdempotencyKey = idempotencyKey;
    }

    public string IdempotencyKey { get; }
}
