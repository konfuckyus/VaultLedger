namespace VaultLedger.Domain.Exceptions;

public sealed class InvalidPinException : DomainException
{
    public InvalidPinException(string message = "İşlem PIN'i hatalı.")
        : base(message)
    {
    }
}
