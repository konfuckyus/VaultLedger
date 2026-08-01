namespace VaultLedger.Application.Exceptions;

public sealed class CardNumberGenerationException : AppException
{
    public CardNumberGenerationException(string message)
        : base(message)
    {
    }
}
