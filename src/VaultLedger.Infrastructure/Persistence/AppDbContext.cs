using Microsoft.EntityFrameworkCore;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<TransactionRecord> TransactionRecords => Set<TransactionRecord>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AccountRequest> AccountRequests => Set<AccountRequest>();
    public DbSet<CardRequest> CardRequests => Set<CardRequest>();
    public DbSet<TopUpRequest> TopUpRequests => Set<TopUpRequest>();
    public DbSet<BudgetCategoryDefinition> BudgetCategoryDefinitions => Set<BudgetCategoryDefinition>();
    public DbSet<CategoryEligibility> CategoryEligibilities => Set<CategoryEligibility>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
