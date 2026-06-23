using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Pdf;
using Osiris.Infrastructure.AI.Gemini;
using Osiris.Infrastructure.AI.Telemetry;
using Osiris.Infrastructure.Common;
using Osiris.Infrastructure.Email;
using Osiris.Infrastructure.Gemini;
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
        services.AddScoped<ICsvImportPreferenceRepository, CsvImportPreferenceRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ICreditCardPurchaseRepository, CreditCardPurchaseRepository>();
        services.AddScoped<ICreditCardInstallmentRepository, CreditCardInstallmentRepository>();
        services.AddScoped<ICreditCardStatementRepository, CreditCardStatementRepository>();
        services.AddScoped<ICreditCardStatementPaymentRepository, CreditCardStatementPaymentRepository>();
        services.AddScoped<IBillRepository, BillRepository>();
        services.AddScoped<IAiConversationRepository, AiConversationRepository>();
        services.AddScoped<IAiActionProposalRepository, AiActionProposalRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IEmailSender, NoOpEmailSender>();
        services.AddSingleton<IFinancialAccountStatementPdfRenderer, FinancialAccountStatementPdfRenderer>();
        services.AddSingleton<ICreditCardStatementPdfRenderer, CreditCardStatementPdfRenderer>();
        services.AddSingleton<ICashFlowReportPdfRenderer, CashFlowReportPdfRenderer>();

        // AI statement extraction (Gemini) — the first outbound HTTP client in the backend.
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.AddHttpClient<IPdfStatementExtractor, GeminiPdfStatementExtractor>((serviceProvider, client) =>
        {
            var geminiOptions = serviceProvider.GetRequiredService<IOptions<GeminiOptions>>().Value;
            client.BaseAddress = new Uri(geminiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(geminiOptions.TimeoutSeconds);
        });

        // AI assistant: Application-owned options/flags bound here, plus the redactor and the Gemini
        // conversational model client (separate HTTP client/timeout from PDF extraction).
        services.Configure<AiAgentOptions>(configuration.GetSection(AiAgentOptions.SectionName));
        services.Configure<AiFeatureOptions>(configuration.GetSection(AiFeatureOptions.SectionName));
        services.AddSingleton<IAiDataRedactor, AiDataRedactor>();
        services.AddHttpClient<IAiModelClient, GeminiAiModelClient>((serviceProvider, client) =>
        {
            var geminiOptions = serviceProvider.GetRequiredService<IOptions<GeminiOptions>>().Value;
            client.BaseAddress = new Uri(geminiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(geminiOptions.RequestTimeoutSeconds);
        });

        return services;
    }
}
