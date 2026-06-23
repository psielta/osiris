namespace Osiris.Application.Common.AI;

/// <summary>
/// Masks secrets and personal identifiers (API keys, JWTs, connection strings, e-mails, CPF/CNPJ)
/// before tool arguments/results are persisted or logged. Implemented in Infrastructure.
/// </summary>
public interface IAiDataRedactor
{
    string Redact(string? text);
}
