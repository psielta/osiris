namespace Osiris.Application.Features.AiAssistant.DTOs;

public sealed record AiMessageDto(Guid Id, string Role, string Content, DateTime CreatedAtUtc);

public sealed record AiSourceDto(string Type, string? Id, string Label);

/// <summary>The result of one assistant turn returned to the transport layer.</summary>
public sealed record AiTurnDto(
    Guid ConversationId,
    AiMessageDto Message,
    IReadOnlyList<AiSourceDto> Sources,
    bool UsageLimited);
