using Osiris.Application.Common.AI;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Services;

/// <summary>
/// The execution gate, evaluated independently of the prompt for every tool call the model requests.
/// Even if a tool was somehow offered or named via prompt injection, a write/restricted/forbidden tool
/// will not run here unless policy allows it.
/// </summary>
public sealed class AiToolExecutionPolicy : IAiToolExecutionPolicy
{
    public AiToolDecision Evaluate(AiAgentContext context, IAiTool tool)
    {
        return tool.Risk switch
        {
            AiToolRisk.ReadOnly => AiToolDecision.Allow(),
            AiToolRisk.WriteProposal => context.WritesEnabled
                ? AiToolDecision.Allow()
                : AiToolDecision.Deny("As ferramentas de escrita estão desabilitadas."),
            AiToolRisk.Restricted => AiToolDecision.Deny("Ferramenta restrita não disponível."),
            AiToolRisk.Forbidden => AiToolDecision.Deny("Ferramenta proibida."),
            _ => AiToolDecision.Deny("Risco de ferramenta desconhecido.")
        };
    }
}
