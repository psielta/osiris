using System.Text;
using Osiris.Application.Common.Csv;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Common.Csv;

public sealed class CsvStatementParserTests
{
    // Real Banese/Sicredi-style export: separate Crédito/Débito columns, 10 preamble lines before the
    // header, a balance-only row and a trailing "Total" line that must be dropped.
    private static readonly string[] DebitCreditFile =
    {
        "Extrato de: Ag: 7884 | Conta: 296650-6;;;;;",
        "Data;Histórico;Docto.;Crédito (R$);Débito (R$);Saldo (R$)",
        ";;;;Extrato inexistente;;;;",
        ";;;;;",
        "Filtro de resultados - Movimentação entre:  11/06/2026 e 20/06/2026;;;;;",
        ";;;;;",
        "Os dados acima tem como base 20/06/2026 às 19:35 e estão sujeitos a alterações.;;;;;",
        ";;;;;",
        ";;;;;",
        "Últimos Lancamentos;;;;;",
        "Data;Histórico;Docto.;Crédito (R$);Débito (R$);",
        "08/06/2026;COD. LANC. 0;0; ; ;1.680,00",
        "22/06/2026;PIX QR CODE DINAMICO;1614053; ;1.679,74;0,26",
        "",
        ";;Total;0,00;1.679,74;0,26;",
    };

    // Real Inter/Nubank-style export: single signed Valor column, 5 preamble lines, a Saldo column to ignore.
    private static readonly string[] SignedFile =
    {
        " Extrato Conta Corrente ",
        "Conta ;292678819",
        "Período ;20/05/2026 a 20/06/2026",
        "Saldo ;809,75",
        "",
        "Data Lançamento;Histórico;Descrição;Valor;Saldo",
        "19/06/2026;Pix enviado ;Demerge Brasil Facilitadora De Pagamentos Ltda;-39,99;809,75",
        "19/06/2026;Pix enviado ;Flavio Henrique Silva Oliveira;-2,00;849,74",
        "17/06/2026;Pix recebido;A.g. & R. Solucoes Ltda;100,00;851,74",
        "16/06/2026;Pix recebido;Mateus Augusto Salgueiro Canoas;400,00;751,74",
        "06/06/2026;Pix enviado ;Mateus Augusto Salgueiro Canoas;-1.680,00;351,74",
        "06/06/2026;Pix enviado ;Sbr Sociedade Brasileira De Administracao De Recebiveis Ltda;-129,90;2.031,74",
        "04/06/2026;Pix enviado ;Demerge Brasil Facilitadora De Pagamentos Ltda;-70,99;2.161,64",
        "03/06/2026;Pix recebido;A.g. & R. Solucoes Ltda;4.000,00;2.232,63",
        "03/06/2026;Pagamento efetuado;Pagamento Fatura - MATEUS AUGUSTO SALGUEIRO CANOAS;-591,37;-1.767,37",
        "03/06/2026;Pix enviado ;Mateus Augusto Salgueiro Canoas;-1.006,00;-1.176,00",
        "03/06/2026;Pix enviado ;Larissa Izabela Salgueiro Canoas;-170,00;-170,00",
        "24/05/2026;Pix recebido;Joao Pedro Silva Moreno;100,00;0,00",
        "24/05/2026;Pagamento efetuado;Pagamento Fatura - MATEUS AUGUSTO SALGUEIRO CANOAS;-100,00;-100,00",
        "23/05/2026;Pagamento efetuado;Pagamento Fatura - MATEUS AUGUSTO SALGUEIRO CANOAS;-24,00;0,00",
        "22/05/2026;Pix recebido;Roni Ferreira De Souza;300,00;24,00",
        "22/05/2026;Pagamento efetuado;Pagamento Fatura - MATEUS AUGUSTO SALGUEIRO CANOAS;-300,00;-276,00",
        "22/05/2026;Pix recebido;Leonardo Leonel Dias;600,00;24,00",
        "22/05/2026;Pix enviado ;Roni Ferreira De Souza;-300,00;-576,00",
        "22/05/2026;Pix enviado ;Nanina Pizzaria Ltda;-276,00;-276,00",
    };

    private readonly CsvStatementParser _parser = new();

    [Fact]
    public void Analyze_SignedFile_DetectsSemicolonAndSuggestsHeader()
    {
        var result = _parser.Analyze(Bytes(SignedFile));

        Assert.Equal(";", result.Delimiter);
        Assert.Equal("utf-8", result.Encoding);
        Assert.Equal(5, result.SuggestedHeaderLineIndex);
        Assert.NotEmpty(result.SampleRows);
    }

    [Fact]
    public void Analyze_DebitCreditFile_SuggestsHeaderAfterPreamble()
    {
        var result = _parser.Analyze(Bytes(DebitCreditFile));

        Assert.Equal(10, result.SuggestedHeaderLineIndex);
    }

    [Fact]
    public void Parse_SignedFile_ReadsAllTransactions_SkippingPreambleAndSaldo()
    {
        var transactions = _parser.Parse(Bytes(SignedFile), new CsvImportMapping
        {
            HeaderLineIndex = 5,
            AmountMode = CsvAmountMode.SignedAmount,
            DateColumn = 0,
            DescriptionColumn = 2,
            AmountColumn = 3,
        });

        Assert.Equal(19, transactions.Count);

        var first = transactions[0];
        Assert.Equal(new DateOnly(2026, 6, 19), first.OccurredOn);
        Assert.Equal(FinancialAccountMovementType.Expense, first.Type);
        Assert.Equal(39.99m, first.Amount);
        Assert.Equal("Demerge Brasil Facilitadora De Pagamentos Ltda", first.Description);

        Assert.Contains(transactions, t => t.Type == FinancialAccountMovementType.Income && t.Amount == 100.00m);
        Assert.Contains(transactions, t => t.Amount == 1680.00m); // thousands separator handled
    }

    [Fact]
    public void Parse_DebitCreditFile_MapsDebitToExpense_AndDropsBalanceAndTotalRows()
    {
        var transactions = _parser.Parse(Bytes(DebitCreditFile), new CsvImportMapping
        {
            HeaderLineIndex = 10,
            AmountMode = CsvAmountMode.DebitCredit,
            DateColumn = 0,
            DescriptionColumn = 1,
            CreditColumn = 3,
            DebitColumn = 4,
        });

        var transaction = Assert.Single(transactions);
        Assert.Equal(new DateOnly(2026, 6, 22), transaction.OccurredOn);
        Assert.Equal(FinancialAccountMovementType.Expense, transaction.Type);
        Assert.Equal(1679.74m, transaction.Amount);
        Assert.Equal("PIX QR CODE DINAMICO", transaction.Description);
    }

    [Fact]
    public void Parse_TypeColumn_UsesTokensToDecideDirection()
    {
        string[] file =
        {
            "data;descricao;valor;tipo",
            "01/02/2026;Salario;1500,00;Credito",
            "02/02/2026;Mercado;90,00;Debito",
        };

        var transactions = _parser.Parse(Bytes(file), new CsvImportMapping
        {
            HeaderLineIndex = 0,
            AmountMode = CsvAmountMode.TypeColumn,
            DateColumn = 0,
            DescriptionColumn = 1,
            AmountColumn = 2,
            TypeColumn = 3,
        });

        Assert.Equal(2, transactions.Count);
        Assert.Equal(FinancialAccountMovementType.Income, transactions[0].Type);
        Assert.Equal(1500.00m, transactions[0].Amount);
        Assert.Equal(FinancialAccountMovementType.Expense, transactions[1].Type);
        Assert.Equal(90.00m, transactions[1].Amount);
    }

    [Fact]
    public void Parse_DotDecimalSeparator_IsHonored()
    {
        string[] file =
        {
            "date,description,amount",
            "2026-02-01,Coffee,-3.50",
        };

        var transactions = _parser.Parse(Bytes(file), new CsvImportMapping
        {
            Delimiter = ",",
            HeaderLineIndex = 0,
            AmountMode = CsvAmountMode.SignedAmount,
            DateColumn = 0,
            DescriptionColumn = 1,
            AmountColumn = 2,
            DateFormat = "yyyy-MM-dd",
            DecimalSeparator = ".",
        });

        var transaction = Assert.Single(transactions);
        Assert.Equal(3.50m, transaction.Amount);
        Assert.Equal(FinancialAccountMovementType.Expense, transaction.Type);
    }

    [Fact]
    public void Parse_ExternalIdColumn_IsUsedWhenMapped()
    {
        string[] file =
        {
            "data;descricao;valor;id",
            "01/02/2026;Salario;1500,00;TX-1",
        };

        var mapping = new CsvImportMapping
        {
            HeaderLineIndex = 0,
            DateColumn = 0,
            DescriptionColumn = 1,
            AmountColumn = 2,
            ExternalIdColumn = 3,
        };

        var transaction = Assert.Single(_parser.Parse(Bytes(file), mapping));
        Assert.Equal("TX-1", transaction.ExternalId);
    }

    [Fact]
    public void Parse_SynthesizedExternalId_IsStableAcrossRuns()
    {
        var bytes = Bytes(SignedFile);
        var mapping = new CsvImportMapping
        {
            HeaderLineIndex = 5,
            DateColumn = 0,
            DescriptionColumn = 2,
            AmountColumn = 3,
        };

        var first = _parser.Parse(bytes, mapping);
        var second = _parser.Parse(bytes, mapping);

        Assert.StartsWith("csv-syn:", first[0].ExternalId);
        Assert.Equal(first[0].ExternalId, second[0].ExternalId);
    }

    private static byte[] Bytes(IEnumerable<string> lines) =>
        Encoding.UTF8.GetBytes(string.Join("\n", lines));
}
