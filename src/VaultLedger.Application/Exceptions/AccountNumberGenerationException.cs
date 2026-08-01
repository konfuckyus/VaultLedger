namespace VaultLedger.Application.Exceptions;

public sealed class AccountNumberGenerationException : AppException
{
    public AccountNumberGenerationException(string message)
        : base(message)
    {
    }
}
