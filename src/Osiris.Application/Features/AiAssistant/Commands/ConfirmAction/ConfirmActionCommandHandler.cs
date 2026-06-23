using System.Text.Json;
using FluentValidation;
using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.AiAssistant.DTOs;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Commands.ConfirmAction;

/// <summary>
/// Confirms a proposal and executes its underlying financial command exactly once. Before executing it
/// re-validates the base state (stale detection) and the TTL; after success it records the produced
/// entity. Confirming an already-executed proposal returns the same result (idempotent).
/// </summary>
public sealed class ConfirmActionCommandHandler : IRequestHandler<ConfirmActionCommand, Result<AiActionResultDto>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiActionProposalRepository _proposals;
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfirmActionCommandHandler(
        IAiActionProposalRepository proposals,
        ISender sender,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _proposals = proposals;
        _sender = sender;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AiActionResultDto>> Handle(ConfirmActionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result<AiActionResultDto>.Failure(
                new ResultError("Usuário não autenticado.", null, ResultErrorCodes.Unauthorized));
        }

        var proposal = await _proposals.GetAsync(_currentUser.TenantId, userId, request.Id, cancellationToken);
        if (proposal is null)
        {
            return Result<AiActionResultDto>.Failure(
                new ResultError("Proposta não encontrada.", null, ResultErrorCodes.NotFound));
        }

        // Idempotent: a second confirmation returns the same result without re-executing.
        if (proposal.Status == AiActionProposalStatus.Executed)
        {
            return Result<AiActionResultDto>.Success(Map(proposal));
        }

        if (!proposal.IsPending)
        {
            return Conflict("Esta proposta já foi resolvida.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        if (proposal.IsExpiredOn(utcNow))
        {
            proposal.Expire();
            await _proposals.UpdateAsync(proposal, cancellationToken);
            return Conflict("Esta proposta expirou. Gere uma nova.");
        }

        var staleReason = await RevalidateAsync(proposal, cancellationToken);
        if (staleReason is not null)
        {
            proposal.MarkStale();
            await _proposals.UpdateAsync(proposal, cancellationToken);
            return Conflict(staleReason);
        }

        proposal.Confirm(utcNow);
        proposal.MarkExecuting();
        await _proposals.UpdateAsync(proposal, cancellationToken);

        var (entityType, entityId, failure) = await ExecuteAsync(proposal, cancellationToken);
        if (failure is not null)
        {
            proposal.MarkFailed("command_failed", failure);
            await _proposals.UpdateAsync(proposal, cancellationToken);
            return Result<AiActionResultDto>.Failure(new ResultError(failure));
        }

        proposal.MarkExecuted(entityType!, entityId, utcNow);
        await _proposals.UpdateAsync(proposal, cancellationToken);
        return Result<AiActionResultDto>.Success(Map(proposal));
    }

    private async Task<string?> RevalidateAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        switch (proposal.ActionType)
        {
            case AiActionTypes.ManualMovement:
                var payload = Deserialize(proposal.PayloadJson);
                if (payload is null)
                {
                    return "Não foi possível reler a proposta.";
                }

                var account = await _sender.Send(new GetFinancialAccountDetailsQuery(payload.AccountId), cancellationToken);
                if (account is null)
                {
                    return "A conta da proposta não está mais disponível.";
                }

                var currentHash = ProposalState.AccountHash(account.CurrentBalance, account.IsActive);
                return currentHash == proposal.StateHash
                    ? null
                    : "O estado da conta mudou desde a proposta. Gere uma nova.";

            default:
                return "Tipo de ação não suportado.";
        }
    }

    private async Task<(string? EntityType, Guid? EntityId, string? Failure)> ExecuteAsync(
        AiActionProposal proposal,
        CancellationToken cancellationToken)
    {
        switch (proposal.ActionType)
        {
            case AiActionTypes.ManualMovement:
                var payload = Deserialize(proposal.PayloadJson);
                if (payload is null || !Enum.TryParse<FinancialAccountMovementType>(payload.Type, out var type))
                {
                    return (null, null, "Não foi possível reler a proposta.");
                }

                try
                {
                    var result = await _sender.Send(
                        new CreateManualMovementCommand(
                            payload.AccountId,
                            type,
                            payload.Amount,
                            payload.OccurredOn,
                            payload.Description,
                            payload.CategoryId,
                            payload.Notes),
                        cancellationToken);

                    return result.IsSuccess
                        ? ("FinancialAccountMovement", result.Value, null)
                        : (null, null, result.Errors.FirstOrDefault()?.Message ?? "Não foi possível registrar o lançamento.");
                }
                catch (ValidationException exception)
                {
                    return (null, null, exception.Errors.FirstOrDefault()?.ErrorMessage ?? "Dados do lançamento inválidos.");
                }

            default:
                return (null, null, "Tipo de ação não suportado.");
        }
    }

    private static ManualMovementPayload? Deserialize(string payloadJson) =>
        JsonSerializer.Deserialize<ManualMovementPayload>(payloadJson, SerializerOptions);

    private static AiActionResultDto Map(AiActionProposal proposal) =>
        new(proposal.Id, proposal.Status.ToString(), proposal.ResultEntityType, proposal.ResultEntityId);

    private static Result<AiActionResultDto> Conflict(string message) =>
        Result<AiActionResultDto>.Failure(new ResultError(message, null, ResultErrorCodes.Conflict));
}
