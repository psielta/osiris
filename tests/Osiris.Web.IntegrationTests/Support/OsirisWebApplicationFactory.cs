using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Osiris.Web.IntegrationTests.Support;

public sealed class OsirisWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionStringEnvironmentKey = "ConnectionStrings__DefaultConnection";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("osiris_tests")
        .WithUsername("osiris")
        .WithPassword("osiris")
        .Build();

    private string? _connectionString;

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
        Environment.SetEnvironmentVariable(ConnectionStringEnvironmentKey, _connectionString);
        await ResetDatabaseAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        if (Environment.GetEnvironmentVariable(ConnectionStringEnvironmentKey) == _connectionString)
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvironmentKey, null);
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
                    ?? throw new InvalidOperationException("PostgreSQL test container has not started.")
            });
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
