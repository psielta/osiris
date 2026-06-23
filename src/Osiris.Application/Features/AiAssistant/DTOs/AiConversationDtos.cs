namespace Osiris.Application.Features.AiAssistant.DTOs;

public sealed record AiConversationListItemDto(
    Guid Id,
    string Title,
    string Status,
    DateTime? UpdatedAtUtc,
    DateTime CreatedAtUtc);

public sealed record AiConversationDetailDto(
    Guid Id,
    string Title,
    string Status,
    IReadOnlyList<AiMessageDto> Messages);
