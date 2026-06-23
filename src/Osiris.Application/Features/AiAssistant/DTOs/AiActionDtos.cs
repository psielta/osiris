namespace Osiris.Application.Features.AiAssistant.DTOs;

public sealed record AiActionProposalDto(
    Guid Id,
    string ActionType,
    string DisplaySummary,
    string ImpactSummary,
    string RiskLevel,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    string? ResultEntityType,
    Guid? ResultEntityId);

/// <summary>The outcome of confirming a proposal: the final status and the entity it produced (if any).</summary>
public sealed record AiActionResultDto(
    Guid ProposalId,
    string Status,
    string? ResultEntityType,
    Guid? ResultEntityId);
