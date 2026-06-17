using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Application.Common.Interfaces;
using Osiris.Infrastructure.Common;
using Osiris.Infrastructure.Email;
using Osiris.Infrastructure.Identity;
using Osiris.Infrastructure.Persistence;
using Osiris.Infrastructure.Reporting;

namespace Osiris.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // QuestPDF Community license: free for individuals and organizations under USD 1M revenue.
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddClaimsPrincipalFactory<TenantClaimsPrincipalFactory>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
        });

        services.Configure<RefreshTokenOptions>(configuration.GetSection("Jwt"));

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IFinancialAccountRepository, FinancialAccountRepository>();
        services.AddScoped<IFinancialAccountMovementRepository, FinancialAccountMovementRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ICreditCardPurchaseRepository, CreditCardPurchaseRepository>();
        services.AddScoped<ICreditCardInstallmentRepository, CreditCardInstallmentRepository>();
        services.AddScoped<ICreditCardStatementRepository, CreditCardStatementRepository>();
        services.AddScoped<ICreditCardStatementPaymentRepository, CreditCardStatementPaymentRepository>();
        services.AddScoped<IBillRepository, BillRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IEmailSender, NoOpEmailSender>();
        services.AddSingleton<IFinancialAccountStatementPdfRenderer, FinancialAccountStatementPdfRenderer>();
        services.AddSingleton<ICreditCardStatementPdfRenderer, CreditCardStatementPdfRenderer>();
        services.AddSingleton<ICashFlowReportPdfRenderer, CashFlowReportPdfRenderer>();

        return services;
    }
}
