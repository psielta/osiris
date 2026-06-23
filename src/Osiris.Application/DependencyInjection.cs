using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Behaviors;
using Osiris.Application.Common.Csv;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Ofx;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.AiAssistant.Tools.Proposals;
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

        // Read tools (Section 8.1). Each is whitelisted; the registry/policy gate exposure and execution.
        services.AddScoped<IAiTool, GetFinancialSnapshotTool>();
        services.AddScoped<IAiTool, ListFinancialAccountsTool>();
        services.AddScoped<IAiTool, GetAccountStatementTool>();
        services.AddScoped<IAiTool, SearchAccountMovementsTool>();
        services.AddScoped<IAiTool, GetSpendingSummaryTool>();
        services.AddScoped<IAiTool, GetCashFlowSummaryTool>();
        services.AddScoped<IAiTool, ListCreditCardsTool>();
        services.AddScoped<IAiTool, GetCardStatementTool>();
        services.AddScoped<IAiTool, GetStatementsOverviewTool>();
        services.AddScoped<IAiTool, SearchCardPurchasesTool>();
        services.AddScoped<IAiTool, ListBillsTool>();
        services.AddScoped<IAiTool, GetUpcomingObligationsTool>();
        services.AddScoped<IAiTool, ListCategoriesTool>();
        services.AddScoped<IAiTool, GetFinancialDefinitionTool>();

        // Write proposal tools (Section 8.2). Offered only when the writes feature is on; they create a
        // confirmable AiActionProposal and never execute the financial command during the turn.
        services.AddScoped<IAiActionProposalFactory, AiActionProposalFactory>();
        services.AddScoped<IAiTool, ProposeManualMovementTool>();
        services.AddScoped<IAiTool, ProposeBillCreationTool>();
        services.AddScoped<IAiTool, ProposeCardPurchaseTool>();
        services.AddScoped<IAiTool, ProposeBillPaymentTool>();
        services.AddScoped<IAiTool, ProposeStatementPaymentTool>();
        services.AddScoped<IAiTool, ProposeCategoryChangeTool>();
        services.AddScoped<IAiTool, ProposeCategoryCreationTool>();
        services.AddScoped<IAiTool, ProposeCategoryUpdateTool>();
        services.AddScoped<IAiTool, ProposeCategoryArchiveTool>();
        services.AddScoped<IAiTool, ProposeCategoryDeletionTool>();
        services.AddScoped<IAiTool, ProposeAccountCreationTool>();
        services.AddScoped<IAiTool, ProposeAccountUpdateTool>();
        services.AddScoped<IAiTool, ProposeAccountArchiveTool>();
        services.AddScoped<IAiTool, ProposeCardCreationTool>();
        services.AddScoped<IAiTool, ProposeCardUpdateTool>();
        services.AddScoped<IAiTool, ProposeCardArchiveTool>();
        services.AddScoped<IAiTool, ProposeBillUpdateTool>();
        services.AddScoped<IAiTool, ProposeBillDeletionTool>();
        services.AddScoped<IAiTool, ProposeBillUnpayTool>();
        services.AddScoped<IAiTool, ProposePurchaseDeletionTool>();

        return services;
    }
}
