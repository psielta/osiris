using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Osiris.Domain.Entities;
using Osiris.Infrastructure.Identity;

namespace Osiris.Infrastructure.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();

    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

    public DbSet<FinancialAccountMovement> FinancialAccountMovements => Set<FinancialAccountMovement>();

    public DbSet<CsvImportPreference> CsvImportPreferences => Set<CsvImportPreference>();

    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    public DbSet<CreditCardPurchase> CreditCardPurchases => Set<CreditCardPurchase>();

    public DbSet<CreditCardInstallment> CreditCardInstallments => Set<CreditCardInstallment>();

    public DbSet<CreditCardStatement> CreditCardStatements => Set<CreditCardStatement>();

    public DbSet<CreditCardStatementPayment> CreditCardStatementPayments => Set<CreditCardStatementPayment>();

    public DbSet<Bill> Bills => Set<Bill>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.TenantId)
                .IsRequired();

            entity.HasOne(user => user.Tenant)
                .WithMany()
                .HasForeignKey(user => user.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
