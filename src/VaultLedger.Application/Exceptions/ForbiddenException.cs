namespace VaultLedger.Application.Exceptions;

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Forbidden.")
        : base(message)
    {
    }
}
