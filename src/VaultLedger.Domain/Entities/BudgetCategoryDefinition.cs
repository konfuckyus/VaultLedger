using VaultLedger.Domain.Common;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Domain.Entities;

/// <summary>
/// Admin-managed catalog of budget categories used when opening user accounts.
/// </summary>
public class BudgetCategoryDefinition : BaseEntity
{
    public const int MaxNameLength = 64;

    private BudgetCategoryDefinition()
    {
    }

    private BudgetCategoryDefinition(
        string name,
        decimal defaultAllocatedAmount,
        bool isTransferable,
        bool isSelfRequestable,
        bool isSystemDefault)
    {
        Name = name;
        DefaultAllocatedAmount = defaultAllocatedAmount;
        IsTransferable = isTransferable;
        IsSelfRequestable = isSelfRequestable;
        IsSystemDefault = isSystemDefault;
        IsActive = true;
    }

    public string Name { get; private set; } = null!;
    public decimal DefaultAllocatedAmount { get; private set; }
    public bool IsTransferable { get; private set; }

    /// <summary>
    /// When true, authenticated users may pick this category when submitting an account request.
    /// </summary>
    public bool IsSelfRequestable { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Seeded defaults (e.g. Genel) — name/active cannot be changed.</summary>
    public bool IsSystemDefault { get; private set; }

    public static BudgetCategoryDefinition Create(
        string name,
        decimal defaultAllocatedAmount,
        bool isTransferable,
        bool isSelfRequestable = true,
        bool isSystemDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"Name cannot exceed {MaxNameLength} characters.", nameof(name));

        if (defaultAllocatedAmount < 0m)
            throw new InvalidAmountException("Default allocated amount cannot be negative.");

        return new BudgetCategoryDefinition(
            trimmed,
            defaultAllocatedAmount,
            isTransferable,
            isSelfRequestable,
            isSystemDefault);
    }

    public void UpdateDefaults(decimal defaultAllocatedAmount, bool isTransferable)
    {
        if (defaultAllocatedAmount < 0m)
            throw new InvalidAmountException("Default allocated amount cannot be negative.");

        DefaultAllocatedAmount = defaultAllocatedAmount;
        IsTransferable = isTransferable;
    }

    public void SetSelfRequestable(bool isSelfRequestable) => IsSelfRequestable = isSelfRequestable;

    public void Rename(string name)
    {
        if (IsSystemDefault)
        {
            throw new InvalidAccountOperationException(
                "System default category name cannot be changed.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"Name cannot exceed {MaxNameLength} characters.", nameof(name));

        Name = trimmed;
    }

    public void Deactivate()
    {
        if (IsSystemDefault)
        {
            throw new InvalidAccountOperationException(
                "System default category cannot be deactivated.");
        }

        IsActive = false;
    }

    public void Activate() => IsActive = true;
}
