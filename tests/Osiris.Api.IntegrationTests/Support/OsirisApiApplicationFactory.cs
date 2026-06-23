using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Pdf;
using Osiris.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Osiris.Api.IntegrationTests.Support;

public sealed class OsirisApiApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionStringEnvironmentKey = "ConnectionStrings__DefaultConnection";

    // Program.cs reads the Jwt section BEFORE builder.Build(), so (like the connection string) these
    // must come from environment variables, which the default config provider reads at CreateBuilder time.
    private static readonly Dictionary<string, string?> JwtEnvironment = new()
    {
        ["Jwt__Issuer"] = "https://osiris.api.tests",
        ["Jwt__Audience"] = "osiris.tests",
        ["Jwt__SigningKey"] = "integration-tests-osiris-signing-key-please-keep-32plus-chars-0123456789",
        ["Jwt__AccessTokenMinutes"] = "15",
        ["Jwt__RefreshTokenDays"] = "30"
    };

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("osiris_api_tests")
        .WithUsername("osiris")
        .WithPassword("osiris")
        .Build();

    private string? _connectionString;

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
        Environment.SetEnvironmentVariable(ConnectionStringEnvironmentKey, _connectionString);
        foreach (var (key, value) in JwtEnvironment)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        await ResetDatabaseAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        if (Environment.GetEnvironmentVariable(ConnectionStringEnvironmentKey) == _connectionString)
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvironmentKey, null);
        }

        foreach (var key in JwtEnvironment.Keys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }

        await _postgres.DisposeAsync().AsTask();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
                    ?? throw new InvalidOperationException("PostgreSQL test container has not started."),

                // Enable the assistant by default so its flow can be tested; the "disabled" case overrides this.
                ["Features:AiAssistant"] = "true"
            });
        });

        // Replace the real Gemini clients with deterministic fakes so tests never call the API.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPdfStatementExtractor>();
            services.AddSingleton<IPdfStatementExtractor, FakePdfStatementExtractor>();

            services.RemoveAll<IAiModelClient>();
            services.AddSingleton<IAiModelClient, FakeAiModelClient>();
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }
}
