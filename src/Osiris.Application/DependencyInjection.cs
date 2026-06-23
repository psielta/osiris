using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Behaviors;
using Osiris.Application.Common.Csv;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Ofx;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.AiAssistant.Tools.Read;
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

        services.AddSingleton<IOfxStatementParser, OfxStatementParser>();
        services.AddSingleton<ICsvStatementParser, CsvStatementParser>();

        // AI assistant: the prompt builder and policy are stateless; the tool registry, orchestrator and
        // the tools themselves are scoped because they resolve scoped MediatR/repository dependencies.
        services.AddSingleton<IAiPromptBuilder, AiPromptBuilder>();
        services.AddSingleton<IAiToolExecutionPolicy, AiToolExecutionPolicy>();
        services.AddScoped<IAiToolRegistry, AiToolRegistry>();
        services.AddScoped<IAiAgentOrchestrator, AiAgentOrchestrator>();
        services.AddScoped<IAiTool, GetFinancialSnapshotTool>();

        return services;
    }
}
