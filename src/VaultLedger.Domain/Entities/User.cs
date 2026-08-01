using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Domain.Entities;

public class User : BaseEntity
{
    private readonly List<Account> _accounts = [];

    private User()
    {
    }

    private User(string fullName, string email, string passwordHash, UserRole role)
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? TransactionPinHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }

    public bool HasTransactionPin => !string.IsNullOrEmpty(TransactionPinHash);

    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

    public static User Create(string fullName, string email, string passwordHash, UserRole role = UserRole.User)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User(fullName.Trim(), email.Trim().ToLowerInvariant(), passwordHash, role);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void UpdatePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public void SetTransactionPinHash(string pinHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pinHash);
        TransactionPinHash = pinHash;
    }
}
