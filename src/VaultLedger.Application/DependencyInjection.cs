using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Application.Services;
using VaultLedger.Application.Validators;

namespace VaultLedger.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountOwnershipService, AccountOwnershipService>();
        services.AddScoped<IAccountRequestService, AccountRequestService>();
        services.AddScoped<ICardRequestService, CardRequestService>();
        services.AddScoped<ITopUpRequestService, TopUpRequestService>();
        services.AddScoped<IBudgetCategoryService, BudgetCategoryService>();
        services.AddScoped<ICategoryEligibilityService, CategoryEligibilityService>();
        services.AddSingleton<ITransactionFailureGate, NullTransactionFailureGate>();
        return services;
    }
}
