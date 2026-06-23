using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Exceptions;
using Osiris.Application.Features.AiAssistant.Commands.ArchiveConversation;
using Osiris.Application.Features.AiAssistant.Commands.ConfirmAction;
using Osiris.Application.Features.AiAssistant.Commands.DeleteConversation;
using Osiris.Application.Features.AiAssistant.Commands.RejectAction;
using Osiris.Application.Features.AiAssistant.Commands.SendMessage;
using Osiris.Application.Features.AiAssistant.Commands.SubmitFeedback;
using Osiris.Application.Features.AiAssistant.DTOs;
using Osiris.Application.Features.AiAssistant.Queries.GetConversation;
using Osiris.Application.Features.AiAssistant.Queries.ListConversationProposals;
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
        IReadOnlyCollection<AiActionProposalDto> proposals = Array.Empty<AiActionProposalDto>();
        if (conversation is { } conversationId)
        {
            selected = await _mediator.Send(new GetConversationQuery(conversationId), cancellationToken);
            if (selected is not null)
            {
                proposals = await _mediator.Send(new ListConversationProposalsQuery(selected.Id), cancellationToken);
            }
        }

        return View(new AiAssistantIndexViewModel
        {
            Conversations = conversations,
            Selected = selected,
            Proposals = proposals
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

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        await _mediator.Send(new DeleteConversationCommand(id), cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("messages/{id:guid}/feedback")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(
        Guid id,
        [FromForm] int rating,
        [FromForm] Guid? conversationId,
        CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        var result = await _mediator.Send(new SubmitFeedbackCommand(id, rating, null, null), cancellationToken);
        if (result.IsFailure)
        {
            TempData[ErrorMessageKey] = result.Errors.FirstOrDefault()?.Message
                ?? "Não foi possível registrar o feedback.";
        }

        return RedirectToConversation(conversationId);
    }

    [HttpPost("actions/{id:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAction(
        Guid id,
        [FromForm] Guid? conversationId,
        CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        var result = await _mediator.Send(new ConfirmActionCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            TempData[ErrorMessageKey] = result.Errors.FirstOrDefault()?.Message ?? "Não foi possível confirmar.";
        }

        return RedirectToConversation(conversationId);
    }

    [HttpPost("actions/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectAction(
        Guid id,
        [FromForm] Guid? conversationId,
        CancellationToken cancellationToken)
    {
        if (!_features.AiAssistant)
        {
            return NotFound();
        }

        await _mediator.Send(new RejectActionCommand(id), cancellationToken);
        return RedirectToConversation(conversationId);
    }

    private IActionResult RedirectToConversation(Guid? conversationId)
    {
        return conversationId is { } id
            ? RedirectToAction(nameof(Index), new { conversation = id })
            : RedirectToAction(nameof(Index));
    }
}
