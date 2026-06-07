using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Application.Common;
using Osiris.Infrastructure.Identity;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;
using Osiris.Web.Services;

namespace Osiris.Web.IntegrationTests.Authentication;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class TenantContextTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public TenantContextTests(OsirisWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        return _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GeneratedPrincipal_ShouldCarryTenantIdClaim()
    {
        await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users.SingleAsync();
        var claimsFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();

        var principal = await claimsFactory.CreateAsync(user);

        Assert.Equal(user.TenantId.ToString(), principal.FindFirstValue(OsirisClaimTypes.TenantId));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void CurrentUser_ShouldReadTenantUserAndAuthenticationState()
    {
        var tenantId = Guid.NewGuid();
        var userId = "user-123";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(OsirisClaimTypes.TenantId, tenantId.ToString())
        };
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal(tenantId, currentUser.TenantId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void CurrentUser_WhenTenantClaimMissing_ShouldThrow()
    {
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        Assert.False(currentUser.IsAuthenticated);
        Assert.Throws<InvalidOperationException>(() => currentUser.TenantId);
    }
}
