namespace Osiris.Application.Common.AI;

/// <summary>
/// Server-side context for a single turn. Every value here is established from <c>ICurrentUser</c> and
/// configuration — never from anything the model produced. Tools and policy read this; the model
/// cannot influence <see cref="TenantId"/>/<see cref="UserId"/> because they are not part of any tool schema.
/// </summary>
public sealed record AiAgentContext(
    Guid TenantId,
    string UserId,
    Guid ConversationId,
    string CorrelationId,
    DateOnly Today,
    bool WritesEnabled);
