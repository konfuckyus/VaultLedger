namespace VaultLedger.Domain.Exceptions;

public sealed class InvalidAccountOperationException : DomainException
{
    public InvalidAccountOperationException(string message) : base(message)
    {
    }
}
