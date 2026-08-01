namespace VaultLedger.Domain.Exceptions;

public sealed class InvalidRequestOperationException : DomainException
{
    public InvalidRequestOperationException(string message) : base(message)
    {
    }
}
