using System.Globalization;
using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Application.Features.FinancialAccounts.DTOs;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>Shared helpers for the financial-account write-proposal tools.</summary>
internal static class AccountProposalSupport
{
    public static string? ParseType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "checking" or "corrente" or "conta corrente" => FinancialAccountType.CheckingAccount.ToString(),
        "savings" or "poupanca" or "poupança" => FinancialAccountType.SavingsAccount.ToString(),
        "cash" or "dinheiro" or "espécie" or "especie" => FinancialAccountType.Cash.ToString(),
        "other" or "outro" or "outra" => FinancialAccountType.Other.ToString(),
        _ => null
    };

    public static string TypeLabel(FinancialAccountType type) => type switch
    {
        FinancialAccountType.CheckingAccount => "conta corrente",
        FinancialAccountType.SavingsAccount => "poupança",
        FinancialAccountType.Cash => "dinheiro",
        _ => "outra"
    };

    public static async Task<FinancialAccountListItemDto?> FindAsync(ISender sender, Guid id, CancellationToken ct)
    {
        var accounts = await sender.Send(new ListFinancialAccountsQuery(IncludeArchived: true), ct);
        return accounts.FirstOrDefault(account => account.Id == id);
    }
}

/// <summary>Write tool: proposes creating a financial account. Persists a proposal only.</summary>
public sealed class ProposeAccountCreationTool : IAiTool
{
    private readonly IAiActionProposalFactory _factory;

    public ProposeAccountCreationTool(IAiActionProposalFactory factory) => _factory = factory;

    public string Name => "propose_account_creation";

    public string Description =>
        "Cria uma PROPOSTA de nova conta (bancária, poupança, dinheiro). NÃO registra: o usuário confirma depois. "
        + "Informe name e type (checking = corrente, savings = poupança, cash = dinheiro, other = outra); "
        + "initialBalance é opcional (padrão 0).";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string", description = "Nome da conta." },
            type = new { type = "string", description = "checking, savings, cash ou other." },
            initialBalance = new { type = "number", description = "Saldo inicial. Opcional; padrão 0." }
        },
        required = new[] { "name", "type" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var name = AiToolArguments.GetString(arguments, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return AiToolResult.Failure("Informe o nome da conta.");
        }

        var type = AccountProposalSupport.ParseType(AiToolArguments.GetString(arguments, "type"));
        if (type is null)
        {
            return AiToolResult.Failure("Tipo inválido: use checking, savings, cash ou other.");
        }

        var initialBalance = AiToolArguments.GetDecimal(arguments, "initialBalance") ?? 0m;

        var payload = new AccountCreationPayload(name.Trim(), type, initialBalance);
        var label = AccountProposalSupport.TypeLabel(Enum.Parse<FinancialAccountType>(type));
        var display = $"Criar conta \"{name.Trim()}\" ({label}), saldo inicial {ProposalFormatting.Money(initialBalance)}";
        var impact = "Adiciona uma nova conta com o saldo inicial informado.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.AccountCreation, payload, display, impact, cancellationToken);
    }
}

/// <summary>Write tool: proposes editing an account's name or type. Persists a proposal only.</summary>
public sealed class ProposeAccountUpdateTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeAccountUpdateTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_account_update";

    public string Description =>
        "Cria uma PROPOSTA de edição de uma conta (renomear ou mudar o tipo). NÃO registra: o usuário confirma "
        + "depois. Informe accountId (de list_financial_accounts); name e type são opcionais. O saldo não é editado "
        + "aqui (ele decorre dos lançamentos).";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            accountId = new { type = "string", description = "Id da conta (GUID)." },
            name = new { type = "string", description = "Novo nome. Opcional." },
            type = new { type = "string", description = "Novo tipo: checking, savings, cash ou other. Opcional." }
        },
        required = new[] { "accountId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var accountId = AiToolArguments.GetGuid(arguments, "accountId");
        if (accountId is null)
        {
            return AiToolResult.Failure("Informe um accountId válido.");
        }

        var current = await AccountProposalSupport.FindAsync(_sender, accountId.Value, cancellationToken);
        if (current is null)
        {
            return AiToolResult.Failure("Conta não encontrada.");
        }

        var name = AiToolArguments.GetString(arguments, "name")?.Trim();
        var finalName = string.IsNullOrWhiteSpace(name) ? current.Name : name;

        var typeArg = AiToolArguments.GetString(arguments, "type");
        var finalType = current.Type.ToString();
        if (!string.IsNullOrWhiteSpace(typeArg))
        {
            var parsed = AccountProposalSupport.ParseType(typeArg);
            if (parsed is null)
            {
                return AiToolResult.Failure("Tipo inválido: use checking, savings, cash ou other.");
            }

            finalType = parsed;
        }

        var payload = new AccountUpdatePayload(current.Id, finalName, finalType);
        var stateHash = ProposalState.AccountProfileHash(current.Name, current.Type, current.IsActive);
        var display = $"Editar a conta \"{current.Name}\" (novo nome: \"{finalName}\", tipo: {AccountProposalSupport.TypeLabel(Enum.Parse<FinancialAccountType>(finalType))})";
        var impact = "Atualiza o cadastro da conta; o saldo e os lançamentos não mudam.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.AccountUpdate, payload, display, impact, stateHash, cancellationToken);
    }
}

/// <summary>Write tool: proposes archiving (hiding) a financial account. Persists a proposal only.</summary>
public sealed class ProposeAccountArchiveTool : IAiTool
{
    private readonly ISender _sender;
    private readonly IAiActionProposalFactory _factory;

    public ProposeAccountArchiveTool(ISender sender, IAiActionProposalFactory factory)
    {
        _sender = sender;
        _factory = factory;
    }

    public string Name => "propose_account_archive";

    public string Description =>
        "Cria uma PROPOSTA de arquivamento de uma conta (deixa de aparecer para novos lançamentos, sem apagar o "
        + "histórico). NÃO registra: o usuário confirma depois. Informe accountId.";

    public AiToolRisk Risk => AiToolRisk.WriteProposal;

    public object InputSchema => new
    {
        type = "object",
        properties = new { accountId = new { type = "string", description = "Id da conta (GUID)." } },
        required = new[] { "accountId" }
    };

    public async Task<AiToolResult> ExecuteAsync(JsonElement arguments, AiAgentContext context, CancellationToken cancellationToken)
    {
        var accountId = AiToolArguments.GetGuid(arguments, "accountId");
        if (accountId is null)
        {
            return AiToolResult.Failure("Informe um accountId válido.");
        }

        var current = await AccountProposalSupport.FindAsync(_sender, accountId.Value, cancellationToken);
        if (current is null)
        {
            return AiToolResult.Failure("Conta não encontrada.");
        }

        if (!current.IsActive)
        {
            return AiToolResult.Failure("A conta já está arquivada.");
        }

        var payload = new AccountRefPayload(current.Id);
        var stateHash = ProposalState.AccountProfileHash(current.Name, current.Type, current.IsActive);
        var display = $"Arquivar a conta \"{current.Name}\" (saldo atual {ProposalFormatting.Money(current.CurrentBalance)})";
        var impact = "A conta deixa de aparecer para novos lançamentos; o histórico é preservado.";

        return await WriteProposal.PersistAsync(
            _factory, context, AiActionTypes.AccountArchive, payload, display, impact, stateHash, cancellationToken);
    }
}
