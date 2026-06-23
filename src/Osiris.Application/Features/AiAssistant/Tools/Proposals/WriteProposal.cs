using Osiris.Application.Common.AI;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.AiAssistant.Services;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Proposals;

/// <summary>
/// Shared tail for write tools: serialize the payload, persist the proposal via the factory and shape
/// the tool result + proposal reference. Creation actions hash the payload; mutation actions pass their
/// own state hash so confirmation can detect staleness.
/// </summary>
internal static class WriteProposal
{
    /// <summary>For creation actions: the state hash is derived from the payload (nothing to go stale).</summary>
    public static Task<AiToolResult> PersistAsync(
        IAiActionProposalFactory factory,
        AiAgentContext context,
        string actionType,
        object payload,
        string displaySummary,
        string impactSummary,
        CancellationToken cancellationToken)
    {
        var payloadJson = AiToolJson.Serialize(payload);
        return PersistJsonAsync(
            factory, context, actionType, payloadJson, displaySummary, impactSummary,
            ProposalState.PayloadHash(payloadJson), cancellationToken);
    }

    /// <summary>For mutation actions: the caller passes the hash of the target entity's current state.</summary>
    public static Task<AiToolResult> PersistAsync(
        IAiActionProposalFactory factory,
        AiAgentContext context,
        string actionType,
        object payload,
        string displaySummary,
        string impactSummary,
        string stateHash,
        CancellationToken cancellationToken)
    {
        var payloadJson = AiToolJson.Serialize(payload);
        return PersistJsonAsync(
            factory, context, actionType, payloadJson, displaySummary, impactSummary, stateHash, cancellationToken);
    }

    private static async Task<AiToolResult> PersistJsonAsync(
        IAiActionProposalFactory factory,
        AiAgentContext context,
        string actionType,
        string payloadJson,
        string displaySummary,
        string impactSummary,
        string stateHash,
        CancellationToken cancellationToken)
    {
        var proposal = await factory.CreateAsync(
            context, actionType, payloadJson, displaySummary, impactSummary, stateHash, cancellationToken);

        var resultJson = AiToolJson.Serialize(new
        {
            proposalId = proposal.Id,
            status = "pending_confirmation",
            action = actionType,
            display = displaySummary,
            impact = impactSummary,
            note = "A proposta foi criada e aguarda confirmação do usuário. Não foi registrada ainda."
        });

        var reference = new AiProposalReference(
            proposal.Id,
            actionType,
            displaySummary,
            impactSummary,
            AiToolRisk.WriteProposal.ToString(),
            proposal.ExpiresAtUtc);

        return AiToolResult.Success(resultJson, sources: null, proposals: new[] { reference });
    }
}
