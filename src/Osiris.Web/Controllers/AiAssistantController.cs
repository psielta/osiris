using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Exceptions;
using Osiris.Application.Features.AiAssistant.Commands.ArchiveConversation;
using Osiris.Application.Features.AiAssistant.Commands.SendMessage;
using Osiris.Application.Features.AiAssistant.DTOs;
using Osiris.Application.Features.AiAssistant.Queries.GetConversation;
using Osiris.Application.Features.AiAssistant.Queries.ListConversations;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

/// <summary>
/// Cookie-authenticated web surface for the assistant at <c>/assistant</c>. Thin: it only calls the
/// shared Application commands/queries. The whole controller is gated by the AiAssistant feature flag
/// (404 when off). Uses a post-redirect-get flow; antiforgery protects every mutation.
/// </summary>
[Authorize]
[Route("assistant")]
public sealed class AiAssistantController : AppController
{
    public const string ErrorMessageKey = "AssistantErrorMessage";

    private readonly IMediator _mediator;
    private readonly AiFeatureOptions _features;

    public AiAssistantController(IMediator mediator, IOptions<AiFeatureOptions> features)
    {
        _mediator = mediator;
        _features = features.Value;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? conversation, CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        var conversations = await _mediator.Send(new ListConversationsQuery(), cancellationToken);

        AiConversationDetailDto? selected = null;
        if (conversation is { } conversationId)
        {
            selected = await _mediator.Send(new GetConversationQuery(conversationId), cancellationToken);
        }

        return View(new AiAssistantIndexViewModel
        {
            Conversations = conversations,
            Selected = selected
        });
    }

    [HttpPost("send")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(
        [FromForm] Guid? conversationId,
        [FromForm] string? message,
        CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            TempData[ErrorMessageKey] = "Digite uma mensagem para o assistente.";
            return RedirectToConversation(conversationId);
        }

        try
        {
            var result = await _mediator.Send(new SendAiMessageCommand(conversationId, message), cancellationToken);
            if (result.IsSuccess)
            {
                return RedirectToConversation(result.Value!.ConversationId);
            }

            TempData[ErrorMessageKey] = result.Errors.FirstOrDefault()?.Message
                ?? "Não foi possível enviar a mensagem.";
        }
        catch (ValidationException exception)
        {
            TempData[ErrorMessageKey] = exception.Errors.FirstOrDefault()?.ErrorMessage ?? "Mensagem inválida.";
        }
        catch (AiModelException exception)
        {
            TempData[ErrorMessageKey] = exception.Message;
        }

        return RedirectToConversation(conversationId);
    }

    [HttpPost("{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        await _mediator.Send(new ArchiveConversationCommand(id), cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToConversation(Guid? conversationId)
    {
        return conversationId is { } id
            ? RedirectToAction(nameof(Index), new { conversation = id })
            : RedirectToAction(nameof(Index));
    }
}
