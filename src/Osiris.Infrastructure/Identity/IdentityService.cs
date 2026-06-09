using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Domain.Entities;
using Osiris.Infrastructure.Persistence;

namespace Osiris.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(
        ApplicationDbContext dbContext,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<Result<TenantRegistration>> RegisterTenantAndUserAsync(
        string tenantName,
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return Result<TenantRegistration>.Failure(new ResultError("Este e-mail já está cadastrado.", nameof(email)));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var tenant = new Tenant(tenantName.Trim());
        await _dbContext.Tenants.AddAsync(tenant, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            FullName = fullName.Trim(),
            UserName = email.Trim(),
            Email = email.Trim(),
            TenantId = tenant.Id
        };

        var creationResult = await _userManager.CreateAsync(user, password);
        if (!creationResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<TenantRegistration>.Failure(MapIdentityErrors(creationResult.Errors));
        }

        await transaction.CommitAsync(cancellationToken);
        return Result<TenantRegistration>.Success(new TenantRegistration(user.Id, tenant.Id));
    }

    public async Task<Result> PasswordSignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        var result = await _signInManager.PasswordSignInAsync(
            email.Trim(),
            password,
            rememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return Result.Success();
        }

        if (result.IsLockedOut)
        {
            return Result.Failure(new ResultError("Esta conta está temporariamente bloqueada."));
        }

        return Result.Failure(new ResultError("E-mail ou senha inválidos."));
    }

    public async Task<Result> SignInAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result.Failure(new ResultError("Usuário não encontrado."));
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return Result.Success();
    }

    public async Task<Result> SignOutAsync(CancellationToken cancellationToken)
    {
        await _signInManager.SignOutAsync();
        return Result.Success();
    }

    public async Task<Result<string?>> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return Result<string?>.Success(null);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return Result<string?>.Success(token);
    }

    private static IEnumerable<ResultError> MapIdentityErrors(IEnumerable<IdentityError> errors)
    {
        return errors.Select(error =>
        {
            var field = error.Code.Contains("Email", StringComparison.OrdinalIgnoreCase)
                ? "Email"
                : null;

            return new ResultError(MapIdentityErrorDescription(error), field, error.Code);
        });
    }

    private static string MapIdentityErrorDescription(IdentityError error)
    {
        return error.Code switch
        {
            "DuplicateEmail" => "Este e-mail já está cadastrado.",
            "DuplicateUserName" => "Este e-mail já está cadastrado.",
            "InvalidEmail" => "Informe um e-mail válido.",
            "InvalidUserName" => "Informe um e-mail válido.",
            "PasswordTooShort" => "A senha deve ter pelo menos 6 caracteres.",
            "PasswordRequiresDigit" => "A senha deve ter pelo menos um número.",
            "PasswordRequiresLower" => "A senha deve ter pelo menos uma letra minúscula.",
            "PasswordRequiresUpper" => "A senha deve ter pelo menos uma letra maiúscula.",
            "PasswordRequiresNonAlphanumeric" => "A senha deve ter pelo menos um caractere especial.",
            "PasswordRequiresUniqueChars" => "A senha deve ter mais caracteres diferentes.",
            _ => "Não foi possível concluir a operação. Revise os dados informados."
        };
    }
}
