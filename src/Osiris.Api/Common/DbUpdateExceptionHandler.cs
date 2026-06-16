using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Osiris.Api.Common;

/// <summary>
/// Maps a PostgreSQL foreign-key violation (e.g. deleting a category still referenced by an
/// FK-backed entity such as a bill) to a friendly 409 Conflict instead of a raw 500.
/// </summary>
public sealed class DbUpdateExceptionHandler : IExceptionHandler
{
    private const string ForeignKeyViolation = "23503";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException { InnerException: PostgresException { SqlState: ForeignKeyViolation } })
        {
            return false;
        }

        const string message = "Não é possível excluir uma categoria em uso.";
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = message,
            Detail = message,
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
