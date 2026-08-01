namespace VaultLedger.Application.Services;

public sealed class NullTransactionFailureGate : Interfaces.Services.ITransactionFailureGate
{
    public Task BeforeCommitAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
