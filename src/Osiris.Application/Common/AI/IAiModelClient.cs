namespace Osiris.Application.Common.AI;

/// <summary>
/// The single seam between the agent and the AI provider. Implemented in Infrastructure (Gemini);
/// Application code only ever sees the neutral <see cref="AiModelRequest"/> / <see cref="AiModelTurnResult"/>.
/// </summary>
public interface IAiModelClient
{
    Task<AiModelTurnResult> GenerateAsync(AiModelRequest request, CancellationToken cancellationToken);
}
