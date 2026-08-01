namespace VaultLedger.Application.Interfaces.Services;

/// <summary>
/// Test seam for integration tests that need to fail after debit staging / before commit.
/// Production registration uses <see cref="NullTransactionFailureGate"/>.
/// </summary>
public interface ITransactionFailureGate
{
    /// <summary>Called after debit+credit ledger entries are staged, before Commit.</summary>
    Task BeforeCommitAsync(CancellationToken cancellationToken = default);
}
