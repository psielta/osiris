using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Web.Models;

public sealed class AiAssistantIndexViewModel
{
    public IReadOnlyCollection<AiConversationListItemDto> Conversations { get; init; } =
        Array.Empty<AiConversationListItemDto>();

    public AiConversationDetailDto? Selected { get; init; }
}
