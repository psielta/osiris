using Osiris.Infrastructure.AI.Telemetry;

namespace Osiris.Api.IntegrationTests.AiAssistant;

[Trait("Category", "Unit")]
public sealed class AiDataRedactorTests
{
    private readonly AiDataRedactor _redactor = new();

    [Fact]
    public void Masks_email_addresses()
    {
        var result = _redactor.Redact("Fale com joao.silva@example.com hoje");

        Assert.DoesNotContain("@example.com", result);
        Assert.Contains("[email]", result);
    }

    [Fact]
    public void Masks_jwt_tokens()
    {
        var result = _redactor.Redact("token eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w");

        Assert.Contains("[token]", result);
        Assert.DoesNotContain("eyJhbGci", result);
    }

    [Fact]
    public void Masks_connection_string_password()
    {
        var result = _redactor.Redact("Host=db;Port=5432;Password=s3cr3t!;Database=osiris");

        Assert.DoesNotContain("s3cr3t!", result);
        Assert.Contains("[redacted]", result);
    }

    [Fact]
    public void Masks_formatted_cpf()
    {
        var result = _redactor.Redact("CPF 123.456.789-00 do titular");

        Assert.DoesNotContain("123.456.789-00", result);
        Assert.Contains("[doc]", result);
    }

    [Fact]
    public void Keeps_financial_amounts_intact()
    {
        var result = _redactor.Redact("Saldo projetado de 1234.56 e receita 5000.00");

        Assert.Contains("1234.56", result);
        Assert.Contains("5000.00", result);
    }
}
