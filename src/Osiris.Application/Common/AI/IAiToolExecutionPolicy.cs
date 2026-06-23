namespace Osiris.Application.Common.AI;

/// <summary>The decision of whether a specific tool call may run, kept deliberately outside the prompt.</summary>
public sealed record AiToolDecision(bool IsAllowed, string? Reason = null)
{
    public static AiToolDecision Allow() => new(true);

    public static AiToolDecision Deny(string reason) => new(false, reason);
}

/// <summary>
/// Authorizes a tool execution independently of the prompt and the model. This is the enforcement
/// point that prevents prompt-injected requests from running write or out-of-catalogue tools.
/// </summary>
public interface IAiToolExecutionPolicy
{
    AiToolDecision Evaluate(AiAgentContext context, IAiTool tool);
}
