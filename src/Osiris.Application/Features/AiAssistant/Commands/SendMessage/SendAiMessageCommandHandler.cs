using MediatR;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.AiAssistant.DTOs;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Commands.SendMessage;

/// <summary>
/// Orchestrates one assistant turn and persists it. It loads/creates a tenant- and user-scoped
/// conversation, runs the bounded agent loop, then stores the user message, the assistant reply (with
/// usage and prompt identity) and the redacted tool-call audit rows in a single unit of work.
/// </summary>
public sealed class SendAiMessageCommandHandler : IRequestHandler<SendAiMessageCommand, Result<AiTurnDto>>
{
    private readonly IAiConversationRepository _conversations;
    private readonly IAiAgentOrchestrator _orchestrator;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AiAgentOptions _agentOptions;
    private readonly AiFeatureOptions _featureOptions;

    public SendAiMessageCommandHandler(
        IAiConversationRepository conversations,
        IAiAgentOrchestrator orchestrator,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IOptions<AiAgentOptions> agentOptions,
        IOptions<AiFeatureOptions> featureOptions)
    {
        _conversations = conversations;
        _orchestrator = orchestrator;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _agentOptions = agentOptions.Value;
        _featureOptions = featureOptions.Value;
    }

    public async Task<Result<AiTurnDto>> Handle(SendAiMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result<AiTurnDto>.Failure(
                new ResultError("Usuário não autenticado.", null, ResultErrorCodes.Unauthorized));
        }

        var tenantId = _currentUser.TenantId;
        var utcNow = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);

        // Daily token budget per tenant, computed from persisted usage. Checked before any model call.
        if (_agentOptions.DailyTokenLimitPerTenant > 0)
        {
            var dayStartUtc = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
            var usedToday = await _conversations.SumTokensSinceAsync(tenantId, dayStartUtc, cancellationToken);
            if (usedToday >= _agentOptions.DailyTokenLimitPerTenant)
            {
                return Result<AiTurnDto>.Failure(new ResultError(
                    "Limite diário de uso do assistente atingido. Tente novamente amanhã.",
                    null,
                    ResultErrorCodes.QuotaExceeded));
            }
        }

        AiConversation conversation;
        IReadOnlyList<AiModelMessage> priorMessages;

        if (request.ConversationId is { } conversationId)
        {
            var existing = await _conversations.GetAsync(tenantId, userId, conversationId, cancellationToken);
            if (existing is null || !existing.IsActive)
            {
                return Result<AiTurnDto>.Failure(
                    new ResultError("Conversa não encontrada.", null, ResultErrorCodes.NotFound));
            }

            conversation = existing;
            var history = await _conversations.ListMessagesAsync(
                tenantId,
                conversation.Id,
                _agentOptions.MaxHistoryMessages,
                cancellationToken);
            priorMessages = ToModelMessages(history);
        }
        else
        {
            conversation = new AiConversation(
                tenantId,
                userId,
                BuildTitle(request.Message),
                _agentOptions.PromptVersion);

            // Persist the new conversation before the turn so rows created during it (write proposals)
            // have a valid foreign key target.
            await _conversations.AddAsync(conversation, cancellationToken);
            priorMessages = Array.Empty<AiModelMessage>();
        }

        var context = new AiAgentContext(
            tenantId,
            userId,
            conversation.Id,
            Guid.NewGuid().ToString("n"),
            today,
            _featureOptions.AiAssistantWrites);

        var turn = await _orchestrator.RunAsync(context, priorMessages, request.Message, cancellationToken);

        var userMessage = AiMessage.ForUser(tenantId, conversation.Id, userId, request.Message);
        var assistantMessage = AiMessage.ForAssistant(
            tenantId,
            conversation.Id,
            userId,
            turn.AssistantText,
            turn.ModelName,
            turn.PromptVersion,
            turn.PromptHash,
            turn.Usage.InputTokens,
            turn.Usage.OutputTokens,
            turn.Usage.CachedTokens,
            turn.LatencyMs,
            turn.FinishReason.ToString(),
            context.CorrelationId);

        var toolCalls = turn.ExecutedToolCalls
            .Select(record => new AiToolCall(
                tenantId,
                conversation.Id,
                assistantMessage.Id,
                record.ToolName,
                record.Risk,
                record.Status,
                record.ArgumentsJsonRedacted,
                record.ResultJsonRedacted,
                record.DurationMs,
                record.ErrorCode,
                utcNow))
            .ToList();

        conversation.Touch(utcNow);

        await _conversations.SaveTurnAsync(
            conversation,
            new[] { userMessage, assistantMessage },
            toolCalls,
            cancellationToken);

        var dto = new AiTurnDto(
            conversation.Id,
            new AiMessageDto(assistantMessage.Id, "assistant", assistantMessage.Content, assistantMessage.CreatedAtUtc),
            turn.Sources.Select(source => new AiSourceDto(source.Type, source.Id, source.Label)).ToList(),
            turn.Proposals.Select(proposal => new AiProposalDto(
                proposal.Id,
                proposal.ActionType,
                proposal.DisplaySummary,
                proposal.ImpactSummary,
                proposal.RiskLevel,
                "Pending",
                proposal.ExpiresAtUtc)).ToList(),
            UsageLimited: false);

        return Result<AiTurnDto>.Success(dto);
    }

    private static IReadOnlyList<AiModelMessage> ToModelMessages(IReadOnlyList<AiMessage> stored)
    {
        var messages = new List<AiModelMessage>(stored.Count);
        foreach (var message in stored)
        {
            switch (message.Role)
            {
                case AiMessageRole.User:
                    messages.Add(AiModelMessage.FromUser(message.Content));
                    break;
                case AiMessageRole.Assistant:
                    messages.Add(AiModelMessage.FromAssistant(message.Content));
                    break;
                // Tool messages are audit-only and are not replayed into the model history.
            }
        }

        return messages;
    }

    private static string BuildTitle(string message)
    {
        var title = message.Trim();
        var newlineIndex = title.IndexOf('\n');
        if (newlineIndex >= 0)
        {
            title = title[..newlineIndex].Trim();
        }

        return title.Length > 80 ? title[..80] : title;
    }
}
