using System.Text.Json;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.AiAssistant.Commands.ConfirmAction;
using Osiris.Application.Features.AiAssistant.Proposals;
using Osiris.Application.Features.Bills.Commands.CreateBill;
using Osiris.Application.Features.Bills.Commands.MarkBillAsPaid;
using Osiris.Application.Features.Bills.DTOs;
using Osiris.Application.Features.Bills.Queries.GetBillDetails;
using Osiris.Application.Features.Categories.Commands.CreateCategory;
using Osiris.Application.Features.CreditCardPurchases.Commands.ChangeCreditCardPurchaseCategory;
using Osiris.Application.Features.CreditCardPurchases.DTOs;
using Osiris.Application.Features.CreditCardPurchases.Queries.GetCreditCardPurchaseDetails;
using Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Application.Features.FinancialAccounts.DTOs;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class ConfirmActionCommandHandlerTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTime Now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();

    private AiActionProposal BuildProposal(decimal balanceAtProposalTime, int ttlMinutes = 15)
    {
        var payload = new ManualMovementPayload(_accountId, "Expense", 50m, new DateOnly(2026, 6, 22), "Mercado", null, null);
        return new AiActionProposal(
            _tenantId,
            "user-1",
            Guid.NewGuid(),
            AiActionTypes.ManualMovement,
            JsonSerializer.Serialize(payload, SerializerOptions),
            "Registrar saída",
            "Impacto",
            AiToolRisk.WriteProposal,
            "idem-1",
            ProposalState.AccountHash(balanceAtProposalTime, true),
            Now,
            Now.AddMinutes(ttlMinutes));
    }

    private static FinancialAccountStatementDto Account(Guid id, decimal balance) =>
        new(id, "Conta", FinancialAccountType.CheckingAccount, 0m, balance, true, Array.Empty<MovementListItemDto>());

    private ConfirmActionCommandHandler CreateHandler(FakeAiActionProposalRepository repo, MapSender sender, DateTime? now = null) =>
        new(repo, sender, new FakeCurrentUser(_tenantId), new FakeDateTimeProvider { UtcNow = now ?? Now });

    [Fact]
    public async Task Confirm_executes_underlying_command_once_and_is_idempotent()
    {
        var proposal = BuildProposal(balanceAtProposalTime: 1000m);
        var repo = new FakeAiActionProposalRepository(proposal);
        var movementId = Guid.NewGuid();
        var sender = new MapSender()
            .On<GetFinancialAccountDetailsQuery>(_ => Account(_accountId, 1000m))
            .On<CreateManualMovementCommand>(_ => Result<Guid>.Success(movementId));
        var handler = CreateHandler(repo, sender);

        var result = await handler.Handle(new ConfirmActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Executed", result.Value!.Status);
        Assert.Equal(movementId, result.Value.ResultEntityId);
        Assert.Equal(AiActionProposalStatus.Executed, proposal.Status);
        Assert.Equal(1, sender.Invocations<CreateManualMovementCommand>());

        var again = await handler.Handle(new ConfirmActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(again.IsSuccess);
        Assert.Equal(movementId, again.Value!.ResultEntityId);
        Assert.Equal(1, sender.Invocations<CreateManualMovementCommand>());
    }

    [Fact]
    public async Task Confirm_of_stale_proposal_returns_conflict_and_does_not_execute()
    {
        var proposal = BuildProposal(balanceAtProposalTime: 1000m);
        var repo = new FakeAiActionProposalRepository(proposal);
        var sender = new MapSender()
            .On<GetFinancialAccountDetailsQuery>(_ => Account(_accountId, 2000m)); // balance changed since proposal
        var handler = CreateHandler(repo, sender);

        var result = await handler.Handle(new ConfirmActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.Conflict);
        Assert.Equal(AiActionProposalStatus.Stale, proposal.Status);
        Assert.Equal(0, sender.Invocations<CreateManualMovementCommand>());
    }

    [Fact]
    public async Task Confirm_of_expired_proposal_returns_conflict()
    {
        var proposal = BuildProposal(balanceAtProposalTime: 1000m, ttlMinutes: 15);
        var repo = new FakeAiActionProposalRepository(proposal);
        var sender = new MapSender(); // never reached: expiry is checked before revalidation/execution
        var handler = CreateHandler(repo, sender, now: Now.AddMinutes(20));

        var result = await handler.Handle(new ConfirmActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.Conflict);
        Assert.Equal(AiActionProposalStatus.Expired, proposal.Status);
    }

    [Fact]
    public async Task Confirm_bill_creation_executes_create_bill_once()
    {
        var payload = new BillCreationPayload("Aluguel", 1200m, new DateOnly(2026, 7, 10), Guid.NewGuid(), null, null);
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
        var proposal = new AiActionProposal(
            _tenantId, "user-1", Guid.NewGuid(), AiActionTypes.BillCreation, payloadJson,
            "Criar conta", "Impacto", AiToolRisk.WriteProposal, "idem-bc",
            ProposalState.PayloadHash(payloadJson), Now, Now.AddMinutes(15));
        var repo = new FakeAiActionProposalRepository(proposal);
        var billId = Guid.NewGuid();
        var sender = new MapSender().On<CreateBillCommand>(_ => Result<Guid>.Success(billId));
        var handler = CreateHandler(repo, sender);

        var result = await handler.Handle(new ConfirmActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Executed", result.Value!.Status);
        Assert.Equal(billId, result.Value.ResultEntityId);
        Assert.Equal(1, sender.Invocations<CreateBillCommand>());
    }

    [Fact]
    public async Task Confirm_bill_payment_marks_the_bill_as_paid()
    {
        var billId = Guid.NewGuid();
        const decimal amount = 500m;
        var payload = new BillPaymentPayload(billId, new DateOnly(2026, 6, 22), null);
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
        var proposal = new AiActionProposal(
            _tenantId, "user-1", Guid.NewGuid(), AiActionTypes.BillPayment, payloadJson,
            "Pagar conta", "Impacto", AiToolRisk.WriteProposal, "idem-bp",
            ProposalState.BillHash(null, amount), Now, Now.AddMinutes(15));
        var repo = new FakeAiActionProposalRepository(proposal);
        var bill = new BillDetailsDto(
            billId, "Aluguel", amount, new DateOnly(2026, 6, 10), null, BillStatus.Pending,
            Guid.NewGuid(), null, null, null, null, null, Now, null);
        var sender = new MapSender()
            .On<GetBillDetailsQuery>(_ => bill)
            .On<MarkBillAsPaidCommand>(_ => Result.Success());
        var handler = CreateHandler(repo, sender);

        var result = await handler.Handle(new ConfirmActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Executed", result.Value!.Status);
        Assert.Equal(billId, result.Value.ResultEntityId);
        Assert.Equal(1, sender.Invocations<MarkBillAsPaidCommand>());
    }

    [Fact]
    public async Task Confirm_category_change_executes_change_command_once()
    {
        var purchaseId = Guid.NewGuid();
        var currentCategoryId = Guid.NewGuid();
        var proposal = BuildCategoryChangeProposal(purchaseId, currentCategoryId, Guid.NewGuid());
        var repo = new FakeAiActionProposalRepository(proposal);
        var sender = new MapSender()
            .On<GetCreditCardPurchaseDetailsQuery>(_ => PurchaseDetails(purchaseId, currentCategoryId))
            .On<ChangeCreditCardPurchaseCategoryCommand>(_ => Result.Success());
        var handler = CreateHandler(repo, sender);

        var result = await handler.Handle(new ConfirmActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Executed", result.Value!.Status);
        Assert.Equal(purchaseId, result.Value.ResultEntityId);
        Assert.Equal(1, sender.Invocations<ChangeCreditCardPurchaseCategoryCommand>());
    }

    [Fact]
    public async Task Confirm_category_change_is_stale_when_category_already_changed()
    {
        var purchaseId = Guid.NewGuid();
        var proposal = BuildCategoryChangeProposal(purchaseId, Guid.NewGuid(), Guid.NewGuid());
        var repo = new FakeAiActionProposalRepository(proposal);
        var sender = new MapSender()
            .On<GetCreditCardPurchaseDetailsQuery>(_ => PurchaseDetails(purchaseId, Guid.NewGuid())); // category moved
        var handler = CreateHandler(repo, sender);

        var result = await handler.Handle(new ConfirmActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.Conflict);
        Assert.Equal(AiActionProposalStatus.Stale, proposal.Status);
        Assert.Equal(0, sender.Invocations<ChangeCreditCardPurchaseCategoryCommand>());
    }

    private AiActionProposal BuildCategoryChangeProposal(Guid purchaseId, Guid currentCategoryId, Guid newCategoryId)
    {
        var payload = new CategoryChangePayload(purchaseId, newCategoryId);
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
        return new AiActionProposal(
            _tenantId, "user-1", Guid.NewGuid(), AiActionTypes.CategoryChange, payloadJson,
            "Mudar categoria", "Impacto", AiToolRisk.WriteProposal, "idem-cc",
            ProposalState.PurchaseCategoryHash(currentCategoryId), Now, Now.AddMinutes(15));
    }

    private static CreditCardPurchaseDetailsDto PurchaseDetails(Guid id, Guid categoryId) =>
        new(id, Guid.NewGuid(), "Inter", "Mercado", categoryId, "Farmácia", 80m,
            new DateOnly(2026, 6, 18), 1, null, Array.Empty<CreditCardPurchaseInstallmentDto>());

    [Fact]
    public async Task Confirm_category_creation_executes_create_once()
    {
        var payload = new CategoryCreationPayload("Pets", "Expense", null);
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
        var proposal = new AiActionProposal(
            _tenantId, "user-1", Guid.NewGuid(), AiActionTypes.CategoryCreation, payloadJson,
            "Criar categoria", "Impacto", AiToolRisk.WriteProposal, "idem-cat",
            ProposalState.PayloadHash(payloadJson), Now, Now.AddMinutes(15));
        var repo = new FakeAiActionProposalRepository(proposal);
        var categoryId = Guid.NewGuid();
        var sender = new MapSender().On<CreateCategoryCommand>(_ => Result<Guid>.Success(categoryId));
        var handler = CreateHandler(repo, sender);

        var result = await handler.Handle(new ConfirmActionCommand(proposal.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Executed", result.Value!.Status);
        Assert.Equal(categoryId, result.Value.ResultEntityId);
        Assert.Equal(1, sender.Invocations<CreateCategoryCommand>());
    }

    [Fact]
    public async Task Confirm_of_unknown_proposal_returns_not_found()
    {
        var repo = new FakeAiActionProposalRepository();
        var handler = CreateHandler(repo, new MapSender());

        var result = await handler.Handle(new ConfirmActionCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
    }
}
