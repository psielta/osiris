using System.Text.RegularExpressions;
using Osiris.Application.Common.AI;

namespace Osiris.Infrastructure.AI.Telemetry;

/// <summary>
/// Masks secrets and personal identifiers before tool arguments/results are persisted or logged:
/// JWTs, Google API keys, connection-string passwords, e-mails and formatted CPF/CNPJ. Financial
/// numbers are intentionally left intact — the tool-call audit is controlled storage with retention.
/// </summary>
public sealed partial class AiDataRedactor : IAiDataRedactor
{
    public string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var redacted = JwtRegex().Replace(text, "[token]");
        redacted = GoogleApiKeyRegex().Replace(redacted, "[apikey]");
        redacted = ConnectionSecretRegex().Replace(redacted, "$1=[redacted]");
        redacted = EmailRegex().Replace(redacted, "[email]");
        redacted = DocumentRegex().Replace(redacted, "[doc]");
        return redacted;
    }

    [GeneratedRegex(@"eyJ[A-Za-z0-9_-]{5,}\.[A-Za-z0-9_-]{5,}\.[A-Za-z0-9_-]{5,}")]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"AIza[0-9A-Za-z_\-]{10,}")]
    private static partial Regex GoogleApiKeyRegex();

    [GeneratedRegex(@"(password|pwd)\s*=\s*[^;""']+", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectionSecretRegex();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}|\d{3}\.\d{3}\.\d{3}-\d{2})\b")]
    private static partial Regex DocumentRegex();
}
