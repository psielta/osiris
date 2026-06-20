namespace Osiris.Application.Common.Exceptions;

/// <summary>
/// Raised when the unique index on an account's external ids rejects an insert because the same
/// transactions were imported concurrently. No rows were persisted, so the import should be reported
/// as already-applied rather than failing. Thrown by the persistence layer, handled by the import handler.
/// </summary>
public sealed class DuplicateExternalIdException : Exception
{
    public DuplicateExternalIdException(Exception innerException)
        : base("Os lançamentos já foram importados.", innerException)
    {
    }
}
