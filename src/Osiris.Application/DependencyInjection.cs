using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Application.Common.Behaviors;
using Osiris.Application.Features.Categories.Services;
using Osiris.Application.Features.CreditCardStatements.Services;

namespace Osiris.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        services.AddScoped<CreditCardStatementResolver>();
        services.AddScoped<DefaultFinancialCategoriesSeeder>();

        return services;
    }
}
