using Osiris.Application.Common.AI;

namespace Osiris.Application.UnitTests.Features.AiAssistant.Support;

internal sealed class NoOpAiDataRedactor : IAiDataRedactor
{
    public string Redact(string? text) => text ?? string.Empty;
}
