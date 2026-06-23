using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Common.Models;

namespace Osiris.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Converts a failed <see cref="Result"/> into a JSON ProblemDetails / ValidationProblemDetails,
    /// mapping the error code to the right HTTP status (401 for auth failures, 404 for not-found,
    /// 400 otherwise). Without the code-to-status mapping every business failure would surface as 400.
    /// </summary>
    protected IActionResult Problem(Result result)
    {
        var statusCode = StatusCodeFor(result.Errors);

        var fieldErrors = result.Errors
            .Where(error => !string.IsNullOrWhiteSpace(error.Field))
            .GroupBy(error => NormalizeField(error.Field))
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());

        if (fieldErrors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(fieldErrors)
            {
                Status = statusCode
            });
        }

        var message = result.Errors.FirstOrDefault()?.Message ?? "Não foi possível concluir a operação.";
        return base.Problem(detail: message, statusCode: statusCode);
    }

    private static int StatusCodeFor(IReadOnlyCollection<ResultError> errors)
    {
        if (errors.Any(error => error.Code is ResultErrorCodes.Unauthorized
            or ResultErrorCodes.InvalidRefreshToken
            or ResultErrorCodes.LockedOut))
        {
            return StatusCodes.Status401Unauthorized;
        }

        if (errors.Any(error => error.Code == ResultErrorCodes.NotFound))
        {
            return StatusCodes.Status404NotFound;
        }

        if (errors.Any(error => error.Code == ResultErrorCodes.QuotaExceeded))
        {
            return StatusCodes.Status429TooManyRequests;
        }

        if (errors.Any(error => error.Code == ResultErrorCodes.Conflict))
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status400BadRequest;
    }

    private static string NormalizeField(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(field[0]) + field[1..];
    }
}
