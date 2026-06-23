using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Web.Models;

public sealed class AiAssistantIndexViewModel
{
    public IReadOnlyCollection<AiConversationListItemDto> Conversations { get; init; } =
        Array.Empty<AiConversationListItemDto>();

    public AiConversationDetailDto? Selected { get; init; }

    public IReadOnlyCollection<AiActionProposalDto> Proposals { get; init; } =
        Array.Empty<AiActionProposalDto>();
}

/// <summary>Body posted by the floating assistant widget (JSON fetch).</summary>
public sealed record AssistantWidgetSendRequest(Guid? ConversationId, string? Message);
