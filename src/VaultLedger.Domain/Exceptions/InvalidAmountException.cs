namespace VaultLedger.Domain.Exceptions;

public sealed class InvalidAmountException : DomainException
{
    public InvalidAmountException(string message) : base(message)
    {
    }
}
