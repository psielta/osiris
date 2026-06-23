using System.Text.Json;
using FluentValidation;
using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.AiAssistant.DTOs;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.Bills.Commands.CreateBill;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;
using Osiris.Application.Features.Bills.Queries.GetBillDetails;
using Osiris.Application.Features.Categories.Commands.ArchiveCategory;
using Osiris.Application.Features.Categories.Commands.CreateCategory;
using Osiris.Application.Features.Categories.Commands.DeleteCategory;
using Osiris.Application.Features.Categories.Commands.UpdateCategory;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Application.Features.CreditCardPurchases.Commands.ChangeCreditCardPurchaseCategory;
using Osiris.Application.Features.CreditCardPurchases.Commands.CreateCreditCardPurchase;
using Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;
using Osiris.Application.Features.CreditCardStatementPayments.Commands.RegisterCreditCardStatementPayment;
using Osiris.Application.Features.CreditCardStatements.Queries.GetCreditCardStatementDetails;
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

    private const string StaleMessage = "O estado mudou desde a proposta. Gere uma nova.";
    private const string UnreadableMessage = "Não foi possível reler a proposta.";

    private Task<string?> RevalidateAsync(AiActionProposal proposal, CancellationToken cancellationToken) =>
        proposal.ActionType switch
        {
            AiActionTypes.ManualMovement => RevalidateManualMovementAsync(proposal, cancellationToken),
            AiActionTypes.BillCreation or AiActionTypes.CardPurchase or AiActionTypes.CategoryCreation
                => Task.FromResult(RevalidateCreation(proposal)),
            AiActionTypes.BillPayment => RevalidateBillPaymentAsync(proposal, cancellationToken),
            AiActionTypes.StatementPayment => RevalidateStatementPaymentAsync(proposal, cancellationToken),
            AiActionTypes.CategoryChange => RevalidateCategoryChangeAsync(proposal, cancellationToken),
            AiActionTypes.CategoryUpdate => RevalidateCategoryUpdateAsync(proposal, cancellationToken),
            AiActionTypes.CategoryArchive or AiActionTypes.CategoryDeletion => RevalidateCategoryRefAsync(proposal, cancellationToken),
            _ => Task.FromResult<string?>("Tipo de ação não suportado.")
        };

    private Task<(string? EntityType, Guid? EntityId, string? Failure)> ExecuteAsync(
        AiActionProposal proposal,
        CancellationToken cancellationToken) =>
        proposal.ActionType switch
        {
            AiActionTypes.ManualMovement => ExecuteManualMovementAsync(proposal, cancellationToken),
            AiActionTypes.BillCreation => ExecuteBillCreationAsync(proposal, cancellationToken),
            AiActionTypes.CardPurchase => ExecuteCardPurchaseAsync(proposal, cancellationToken),
            AiActionTypes.BillPayment => ExecuteBillPaymentAsync(proposal, cancellationToken),
            AiActionTypes.StatementPayment => ExecuteStatementPaymentAsync(proposal, cancellationToken),
            AiActionTypes.CategoryChange => ExecuteCategoryChangeAsync(proposal, cancellationToken),
            AiActionTypes.CategoryCreation => ExecuteCategoryCreationAsync(proposal, cancellationToken),
            AiActionTypes.CategoryUpdate => ExecuteCategoryUpdateAsync(proposal, cancellationToken),
            AiActionTypes.CategoryArchive => ExecuteCategoryArchiveAsync(proposal, cancellationToken),
            AiActionTypes.CategoryDeletion => ExecuteCategoryDeletionAsync(proposal, cancellationToken),
            _ => Task.FromResult<(string?, Guid?, string?)>((null, null, "Tipo de ação não suportado."))
        };

    private async Task<string?> RevalidateManualMovementAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<ManualMovementPayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return UnreadableMessage;
        }

        var account = await _sender.Send(new GetFinancialAccountDetailsQuery(payload.AccountId), cancellationToken);
        if (account is null)
        {
            return "A conta da proposta não está mais disponível.";
        }

        return ProposalState.AccountHash(account.CurrentBalance, account.IsActive) == proposal.StateHash
            ? null
            : StaleMessage;
    }

    private static string? RevalidateCreation(AiActionProposal proposal) =>
        ProposalState.PayloadHash(proposal.PayloadJson) == proposal.StateHash ? null : StaleMessage;

    private async Task<string?> RevalidateBillPaymentAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<BillPaymentPayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return UnreadableMessage;
        }

        var bill = await _sender.Send(new GetBillDetailsQuery(payload.BillId), cancellationToken);
        if (bill is null)
        {
            return "A conta a pagar não está mais disponível.";
        }

        return ProposalState.BillHash(bill.PaidAt, bill.Amount) == proposal.StateHash ? null : StaleMessage;
    }

    private async Task<string?> RevalidateStatementPaymentAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<StatementPaymentPayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return UnreadableMessage;
        }

        var statement = await _sender.Send(new GetCreditCardStatementDetailsQuery(payload.StatementId), cancellationToken);
        if (statement is null)
        {
            return "A fatura não está mais disponível.";
        }

        return ProposalState.StatementHash(statement.OpenBalance, statement.Status) == proposal.StateHash
            ? null
            : StaleMessage;
    }

    private async Task<string?> RevalidateCategoryChangeAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<CategoryChangePayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return UnreadableMessage;
        }

        var purchase = await _sender.Send(new GetCreditCardPurchaseDetailsQuery(payload.PurchaseId), cancellationToken);
        if (purchase is null)
        {
            return "A compra não está mais disponível.";
        }

        return ProposalState.PurchaseCategoryHash(purchase.CategoryId) == proposal.StateHash ? null : StaleMessage;
    }

    private Task<string?> RevalidateCategoryUpdateAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<CategoryUpdatePayload>(proposal.PayloadJson);
        return payload is null
            ? Task.FromResult<string?>(UnreadableMessage)
            : CheckCategoryHashAsync(payload.CategoryId, proposal.StateHash, cancellationToken);
    }

    private Task<string?> RevalidateCategoryRefAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<CategoryRefPayload>(proposal.PayloadJson);
        return payload is null
            ? Task.FromResult<string?>(UnreadableMessage)
            : CheckCategoryHashAsync(payload.CategoryId, proposal.StateHash, cancellationToken);
    }

    private async Task<string?> CheckCategoryHashAsync(Guid categoryId, string stateHash, CancellationToken cancellationToken)
    {
        var categories = await _sender.Send(new ListCategoriesQuery(IncludeArchived: true), cancellationToken);
        var category = categories.FirstOrDefault(item => item.Id == categoryId);
        if (category is null)
        {
            return "A categoria não está mais disponível.";
        }

        return ProposalState.CategoryHash(category.Name, category.Type, category.Color, category.IsActive) == stateHash
            ? null
            : StaleMessage;
    }

    private async Task<(string?, Guid?, string?)> ExecuteManualMovementAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<ManualMovementPayload>(proposal.PayloadJson);
        if (payload is null || !Enum.TryParse<FinancialAccountMovementType>(payload.Type, out var type))
        {
            return (null, null, UnreadableMessage);
        }

        return await SendAsync(
            () => _sender.Send(
                new CreateManualMovementCommand(
                    payload.AccountId, type, payload.Amount, payload.OccurredOn,
                    payload.Description, payload.CategoryId, payload.Notes),
                cancellationToken),
            "FinancialAccountMovement");
    }

    private async Task<(string?, Guid?, string?)> ExecuteBillCreationAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<BillCreationPayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return (null, null, UnreadableMessage);
        }

        return await SendAsync(
            () => _sender.Send(
                new CreateBillCommand(
                    payload.Description, payload.Amount, payload.DueDate,
                    payload.CategoryId, payload.PaymentAccountId, payload.Notes),
                cancellationToken),
            "Bill");
    }

    private async Task<(string?, Guid?, string?)> ExecuteCardPurchaseAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<CardPurchasePayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return (null, null, UnreadableMessage);
        }

        return await SendAsync(
            () => _sender.Send(
                new CreateCreditCardPurchaseCommand(
                    payload.CreditCardId, payload.CategoryId, payload.Description,
                    payload.TotalAmount, payload.PurchaseDate, payload.Installments, payload.Notes),
                cancellationToken),
            "CreditCardPurchase");
    }

    private async Task<(string?, Guid?, string?)> ExecuteBillPaymentAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<BillPaymentPayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return (null, null, UnreadableMessage);
        }

        return await SendVoidAsync(
            () => _sender.Send(
                new MarkBillAsPaidCommand(payload.BillId, payload.PaidAt, payload.PaymentAccountId),
                cancellationToken),
            "Bill",
            payload.BillId);
    }

    private async Task<(string?, Guid?, string?)> ExecuteStatementPaymentAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<StatementPaymentPayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return (null, null, UnreadableMessage);
        }

        return await SendAsync(
            () => _sender.Send(
                new RegisterCreditCardStatementPaymentCommand(
                    payload.StatementId, payload.Amount, payload.PaidAt, payload.FinancialAccountId, payload.Notes),
                cancellationToken),
            "CreditCardStatementPayment");
    }

    private async Task<(string?, Guid?, string?)> ExecuteCategoryChangeAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<CategoryChangePayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return (null, null, UnreadableMessage);
        }

        return await SendVoidAsync(
            () => _sender.Send(
                new ChangeCreditCardPurchaseCategoryCommand(payload.PurchaseId, payload.CategoryId),
                cancellationToken),
            "CreditCardPurchase",
            payload.PurchaseId);
    }

    private async Task<(string?, Guid?, string?)> ExecuteCategoryCreationAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<CategoryCreationPayload>(proposal.PayloadJson);
        if (payload is null || !Enum.TryParse<CategoryType>(payload.Type, out var type))
        {
            return (null, null, UnreadableMessage);
        }

        return await SendAsync(
            () => _sender.Send(new CreateCategoryCommand(payload.Name, type, payload.Color), cancellationToken),
            "FinancialCategory");
    }

    private async Task<(string?, Guid?, string?)> ExecuteCategoryUpdateAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<CategoryUpdatePayload>(proposal.PayloadJson);
        if (payload is null || !Enum.TryParse<CategoryType>(payload.Type, out var type))
        {
            return (null, null, UnreadableMessage);
        }

        return await SendVoidAsync(
            () => _sender.Send(new UpdateCategoryCommand(payload.CategoryId, payload.Name, type, payload.Color), cancellationToken),
            "FinancialCategory",
            payload.CategoryId);
    }

    private async Task<(string?, Guid?, string?)> ExecuteCategoryArchiveAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<CategoryRefPayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return (null, null, UnreadableMessage);
        }

        return await SendVoidAsync(
            () => _sender.Send(new ArchiveCategoryCommand(payload.CategoryId), cancellationToken),
            "FinancialCategory",
            payload.CategoryId);
    }

    private async Task<(string?, Guid?, string?)> ExecuteCategoryDeletionAsync(AiActionProposal proposal, CancellationToken cancellationToken)
    {
        var payload = Deserialize<CategoryRefPayload>(proposal.PayloadJson);
        if (payload is null)
        {
            return (null, null, UnreadableMessage);
        }

        return await SendVoidAsync(
            () => _sender.Send(new DeleteCategoryCommand(payload.CategoryId), cancellationToken),
            "FinancialCategory",
            payload.CategoryId);
    }

    private static async Task<(string?, Guid?, string?)> SendAsync(Func<Task<Result<Guid>>> send, string entityType)
    {
        try
        {
            var result = await send();
            return result.IsSuccess ? (entityType, result.Value, null) : (null, null, FirstError(result));
        }
        catch (ValidationException exception)
        {
            return (null, null, exception.Errors.FirstOrDefault()?.ErrorMessage ?? "Dados inválidos.");
        }
    }

    private static async Task<(string?, Guid?, string?)> SendVoidAsync(Func<Task<Result>> send, string entityType, Guid entityId)
    {
        try
        {
            var result = await send();
            return result.IsSuccess ? (entityType, entityId, null) : (null, null, FirstError(result));
        }
        catch (ValidationException exception)
        {
            return (null, null, exception.Errors.FirstOrDefault()?.ErrorMessage ?? "Dados inválidos.");
        }
    }

    private static string FirstError(Result result) =>
        result.Errors.FirstOrDefault()?.Message ?? "Não foi possível concluir a operação.";

    private static T? Deserialize<T>(string payloadJson) =>
        JsonSerializer.Deserialize<T>(payloadJson, SerializerOptions);

    private static AiActionResultDto Map(AiActionProposal proposal) =>
        new(proposal.Id, proposal.Status.ToString(), proposal.ResultEntityType, proposal.ResultEntityId);

    private static Result<AiActionResultDto> Conflict(string message) =>
        Result<AiActionResultDto>.Failure(new ResultError(message, null, ResultErrorCodes.Conflict));
}
