using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Osiris.Api.Contracts;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Exceptions;
using Osiris.Application.Features.AiAssistant.Commands.ArchiveConversation;
using Osiris.Application.Features.AiAssistant.Commands.SendMessage;
using Osiris.Application.Features.AiAssistant.Queries.GetConversation;
using Osiris.Application.Features.AiAssistant.Queries.ListConversations;

namespace Osiris.Api.Controllers.V1;

/// <summary>
/// JWT-protected entry point for one assistant turn. The whole controller is gated by the AiAssistant
/// feature flag: when it is off, every route behaves as if the endpoint does not exist (404).
/// </summary>
[Authorize]
[Route("api/v1/ai")]
public sealed class AiAssistantController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly AiFeatureOptions _features;

    public AiAssistantController(IMediator mediator, IOptions<AiFeatureOptions> features)
    {
        _mediator = mediator;
        _features = features.Value;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        var conversations = await _mediator.Send(new ListConversationsQuery(), cancellationToken);
        return Ok(conversations);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        var conversation = await _mediator.Send(new GetConversationQuery(id), cancellationToken);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpPost("conversations")]
    public Task<IActionResult> Start(SendAiMessageRequest request, CancellationToken cancellationToken) =>
        SendAsync(conversationId: null, request, cancellationToken);

    [HttpPost("conversations/{id:guid}/messages")]
    public Task<IActionResult> Send(Guid id, SendAiMessageRequest request, CancellationToken cancellationToken) =>
        SendAsync(id, request, cancellationToken);

    [HttpPost("conversations/{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        var result = await _mediator.Send(new ArchiveConversationCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result);
    }

    private async Task<IActionResult> SendAsync(
        Guid? conversationId,
        SendAiMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        try
        {
            var result = await _mediator.Send(
                new SendAiMessageCommand(conversationId, request.Message),
                cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : Problem(result);
        }
        catch (AiModelException exception)
        {
            return Problem(detail: exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
