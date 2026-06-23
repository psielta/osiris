using Osiris.Domain.Common;
using Osiris.Domain.Enums;

namespace Osiris.Domain.Entities;

/// <summary>
/// A proposed write the user can confirm or reject. The agent only ever creates these in the
/// <see cref="AiActionProposalStatus.Pending"/> state; the actual financial command runs later,
/// once and only once, when the user confirms via a dedicated endpoint. Write tools are not exposed
/// in this foundation slice, but the schema lives here so confirmation can be added without churn.
/// </summary>
public sealed class AiActionProposal : BaseEntity
{
    private AiActionProposal() { }

    public AiActionProposal(
        Guid tenantId,
        string userId,
        Guid conversationId,
        string actionType,
        string payloadJson,
        string displaySummary,
        string impactSummary,
        AiToolRisk riskLevel,
        string idempotencyKey,
        string stateHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversation id is required.", nameof(conversationId));
        }

        if (string.IsNullOrWhiteSpace(actionType))
        {
            throw new ArgumentException("Action type is required.", nameof(actionType));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        TenantId = tenantId;
        UserId = userId;
        ConversationId = conversationId;
        ActionType = actionType;
        PayloadJson = payloadJson ?? string.Empty;
        DisplaySummary = displaySummary ?? string.Empty;
        ImpactSummary = impactSummary ?? string.Empty;
        RiskLevel = riskLevel;
        IdempotencyKey = idempotencyKey;
        StateHash = stateHash ?? string.Empty;
        Status = AiActionProposalStatus.Pending;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid TenantId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid ConversationId { get; private set; }
    public string ActionType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public string DisplaySummary { get; private set; } = string.Empty;
    public string ImpactSummary { get; private set; } = string.Empty;
    public AiToolRisk RiskLevel { get; private set; }
    public AiActionProposalStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string StateHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? ExecutedAtUtc { get; private set; }
    public string? ResultEntityType { get; private set; }
    public Guid? ResultEntityId { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }

    public bool IsPending => Status == AiActionProposalStatus.Pending;

    public bool IsExpiredOn(DateTime utcNow) => Status == AiActionProposalStatus.Pending && utcNow >= ExpiresAtUtc;

    public void Reject() => TransitionFromPending(AiActionProposalStatus.Rejected);

    public void Expire() => TransitionFromPending(AiActionProposalStatus.Expired);

    public void MarkStale() => TransitionFromPending(AiActionProposalStatus.Stale);

    public void Confirm(DateTime utcNow)
    {
        TransitionFromPending(AiActionProposalStatus.Confirmed);
        ConfirmedAtUtc = utcNow;
    }

    public void MarkExecuting()
    {
        if (Status != AiActionProposalStatus.Confirmed)
        {
            throw new InvalidOperationException("Only a confirmed proposal can start executing.");
        }

        Status = AiActionProposalStatus.Executing;
    }

    public void MarkExecuted(string resultEntityType, Guid? resultEntityId, DateTime utcNow)
    {
        if (Status != AiActionProposalStatus.Executing)
        {
            throw new InvalidOperationException("Only an executing proposal can complete.");
        }

        Status = AiActionProposalStatus.Executed;
        ResultEntityType = resultEntityType;
        ResultEntityId = resultEntityId;
        ExecutedAtUtc = utcNow;
    }

    public void MarkFailed(string failureCode, string failureMessage)
    {
        if (Status != AiActionProposalStatus.Executing)
        {
            throw new InvalidOperationException("Only an executing proposal can fail.");
        }

        Status = AiActionProposalStatus.Failed;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    private void TransitionFromPending(AiActionProposalStatus target)
    {
        if (Status != AiActionProposalStatus.Pending)
        {
            throw new InvalidOperationException($"Proposal is not pending (current status: {Status}).");
        }

        Status = target;
    }
}
