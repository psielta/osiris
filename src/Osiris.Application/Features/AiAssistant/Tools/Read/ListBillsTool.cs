using System.Text.Json;
using MediatR;
using Osiris.Application.Common.AI;
using Osiris.Application.Features.Bills.Queries.ListBills;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Tools.Read;

/// <summary>Lists the off-card payment obligations (contas a pagar) due in a month, optionally by status.</summary>
public sealed class ListBillsTool : IAiTool
{
    private readonly ISender _sender;

    public ListBillsTool(ISender sender)
    {
        _sender = sender;
    }

    public string Name => "list_bills";

    public string Description =>
        "Lista as contas a pagar (obrigações fora do cartão) com vencimento no mês informado. "
        + "Pode filtrar por status (Pending, Paid, Overdue).";

    public AiToolRisk Risk => AiToolRisk.ReadOnly;

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            referenceDate = new { type = "string", description = "Data de referência ISO yyyy-MM-dd (define o mês). Opcional; padrão hoje." },
            status = new { type = "string", description = "Filtra por status: Pending, Paid ou Overdue. Opcional." }
        }
    };

    public async Task<AiToolResult> ExecuteAsync(
        JsonElement arguments,
        AiAgentContext context,
        CancellationToken cancellationToken)
    {
        var (year, month) = AiToolArguments.ResolveMonth(arguments, context.Today);
        var status = AiToolArguments.GetString(arguments, "status");

        var bills = await _sender.Send(new ListBillsQuery(year, month), cancellationToken);

        var filtered = bills
            .Where(bill => string.IsNullOrWhiteSpace(status)
                || string.Equals(bill.Status.ToString(), status, StringComparison.OrdinalIgnoreCase))
            .Select(bill => new
            {
                bill.Id,
                bill.Description,
                bill.Amount,
                bill.DueDate,
                status = bill.Status.ToString(),
                category = bill.CategoryName,
                bill.PaidAt
            })
            .ToList();

        var payload = new
        {
            period = new { year, month },
            bills = filtered,
            billCount = filtered.Count,
            openTotal = filtered.Where(bill => bill.PaidAt is null).Sum(bill => bill.Amount)
        };

        var sources = bills.Select(bill => new AiSource("bill", bill.Id.ToString(), bill.Description)).ToList();
        return AiToolResult.Success(AiToolJson.Serialize(payload), sources);
    }
}
