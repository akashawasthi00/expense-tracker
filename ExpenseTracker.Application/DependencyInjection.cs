using ExpenseTracker.Application.Services.Abstractions;
using ExpenseTracker.Application.Services.Implementations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpenseTracker.Application;

public static class DependencyInjection
{
    /// <summary>Registers the application services, validators, and a system clock.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IReportService, ReportService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        // Injectable, testable clock (services depend on TimeProvider rather than DateTime.UtcNow).
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
