namespace Osiris.Application.Common.Models;

/// <summary>
/// A rendered file ready to be streamed back to the client (e.g. a generated PDF report).
/// </summary>
public sealed record FileExportResult(byte[] Content, string FileName, string ContentType = "application/pdf");
