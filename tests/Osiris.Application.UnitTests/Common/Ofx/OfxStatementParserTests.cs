using System.Text;
using Osiris.Application.Common.Ofx;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Common.Ofx;

public sealed class OfxStatementParserTests
{
    private readonly OfxStatementParser _parser = new();

    [Fact]
    public void Parse_Ofx1Sgml_ReadsTransactionsHeaderAndDecodesCp1252()
    {
        const string sgml = """
            OFXHEADER:100
            DATA:OFXSGML
            VERSION:102
            SECURITY:NONE
            ENCODING:USASCII
            CHARSET:1252
            COMPRESSION:NONE
            OLDFILEUID:NONE
            NEWFILEUID:NONE

            <OFX>
            <BANKMSGSRSV1>
            <STMTTRNRS>
            <TRNUID>1001
            <STMTRS>
            <CURDEF>BRL
            <BANKACCTFROM>
            <BANKID>341
            <ACCTID>12345-6
            <ACCTTYPE>CHECKING
            </BANKACCTFROM>
            <BANKTRANLIST>
            <DTSTART>20260601
            <DTEND>20260630
            <STMTTRN>
            <TRNTYPE>CREDIT
            <DTPOSTED>20260602
            <TRNAMT>1500.00
            <FITID>2026060201
            <MEMO>Salário mensal
            </STMTTRN>
            <STMTTRN>
            <TRNTYPE>DEBIT
            <DTPOSTED>20260605
            <TRNAMT>-89.90
            <FITID>2026060502
            <MEMO>Mercado São José
            </STMTTRN>
            </BANKTRANLIST>
            <LEDGERBAL>
            <BALAMT>1410.10
            <DTASOF>20260630
            </LEDGERBAL>
            </STMTRS>
            </STMTTRNRS>
            </BANKMSGSRSV1>
            </OFX>
            """;

        var statement = _parser.Parse(Cp1252(sgml));

        Assert.Equal("341", statement.BankId);
        Assert.Equal("12345-6", statement.AccountId);
        Assert.Equal("BRL", statement.CurrencyCode);
        Assert.Equal(new DateOnly(2026, 6, 1), statement.StartDate);
        Assert.Equal(new DateOnly(2026, 6, 30), statement.EndDate);
        Assert.Equal(1410.10m, statement.LedgerBalance);

        Assert.Equal(2, statement.Transactions.Count);

        var income = statement.Transactions[0];
        Assert.Equal(FinancialAccountMovementType.Income, income.Type);
        Assert.Equal(1500.00m, income.Amount);
        Assert.Equal(new DateOnly(2026, 6, 2), income.OccurredOn);
        Assert.Equal("2026060201", income.ExternalId);
        Assert.Equal("Salário mensal", income.Description);

        var expense = statement.Transactions[1];
        Assert.Equal(FinancialAccountMovementType.Expense, expense.Type);
        Assert.Equal(89.90m, expense.Amount);
        Assert.Equal("Mercado São José", expense.Description);
    }

    [Fact]
    public void Parse_Ofx2Xml_ReadsClosedTagsAndUtf8()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <?OFX OFXHEADER="200" VERSION="220" SECURITY="NONE" OLDFILEUID="NONE" NEWFILEUID="NONE"?>
            <OFX>
              <BANKMSGSRSV1>
                <STMTTRNRS>
                  <TRNUID>1</TRNUID>
                  <STMTRS>
                    <CURDEF>BRL</CURDEF>
                    <BANKACCTFROM><BANKID>001</BANKID><ACCTID>9999</ACCTID><ACCTTYPE>CHECKING</ACCTTYPE></BANKACCTFROM>
                    <BANKTRANLIST>
                      <DTSTART>20260101</DTSTART>
                      <DTEND>20260131</DTEND>
                      <STMTTRN><TRNTYPE>CREDIT</TRNTYPE><DTPOSTED>20260110120000</DTPOSTED><TRNAMT>200.50</TRNAMT><FITID>ABC123</FITID><NAME>Transferência recebida</NAME></STMTTRN>
                    </BANKTRANLIST>
                  </STMTRS>
                </STMTTRNRS>
              </BANKMSGSRSV1>
            </OFX>
            """;

        var statement = _parser.Parse(Encoding.UTF8.GetBytes(xml));

        var transaction = Assert.Single(statement.Transactions);
        Assert.Equal(FinancialAccountMovementType.Income, transaction.Type);
        Assert.Equal(200.50m, transaction.Amount);
        Assert.Equal(new DateOnly(2026, 1, 10), transaction.OccurredOn);
        Assert.Equal("ABC123", transaction.ExternalId);
        Assert.Equal("Transferência recebida", transaction.Description);
    }

    [Fact]
    public void Parse_WhenFitidMissing_SynthesizesStableKeyAndParsesCommaDecimal()
    {
        const string sgml = """
            OFXHEADER:100
            DATA:OFXSGML
            ENCODING:USASCII
            CHARSET:1252

            <OFX>
            <BANKTRANLIST>
            <STMTTRN>
            <TRNTYPE>DEBIT
            <DTPOSTED>20260315
            <TRNAMT>-1.234,56
            <MEMO>Compra parcelada
            </STMTTRN>
            </BANKTRANLIST>
            </OFX>
            """;

        var first = _parser.Parse(Cp1252(sgml)).Transactions.Single();
        var second = _parser.Parse(Cp1252(sgml)).Transactions.Single();

        Assert.Equal(1234.56m, first.Amount);
        Assert.Equal(FinancialAccountMovementType.Expense, first.Type);
        Assert.StartsWith("ofx-syn:", first.ExternalId);
        Assert.Equal(first.ExternalId, second.ExternalId);
    }

    [Fact]
    public void Parse_SkipsTransactionsWithoutAmountOrDate()
    {
        const string sgml = """
            <OFX>
            <BANKTRANLIST>
            <STMTTRN>
            <TRNTYPE>DEBIT
            <DTPOSTED>20260101
            <TRNAMT>0.00
            <FITID>ZERO
            </STMTTRN>
            <STMTTRN>
            <TRNTYPE>CREDIT
            <TRNAMT>10.00
            <FITID>NODATE
            </STMTTRN>
            <STMTTRN>
            <TRNTYPE>CREDIT
            <DTPOSTED>20260102
            <TRNAMT>25.00
            <FITID>OK
            </STMTTRN>
            </BANKTRANLIST>
            </OFX>
            """;

        var statement = _parser.Parse(Encoding.UTF8.GetBytes(sgml));

        var transaction = Assert.Single(statement.Transactions);
        Assert.Equal("OK", transaction.ExternalId);
    }

    [Fact]
    public void Parse_WhenNotOfx_ReturnsEmpty()
    {
        var statement = _parser.Parse(Encoding.UTF8.GetBytes("isto não é um arquivo ofx"));

        Assert.Empty(statement.Transactions);
    }

    [Fact]
    public void Parse_WhenEmpty_ReturnsEmpty()
    {
        var statement = _parser.Parse([]);

        Assert.Empty(statement.Transactions);
    }

    private static byte[] Cp1252(string text)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252).GetBytes(text);
    }
}
