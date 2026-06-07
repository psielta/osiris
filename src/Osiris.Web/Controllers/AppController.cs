using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Common.Models;

namespace Osiris.Web.Controllers;

public abstract class AppController : Controller
{
    protected void AddResultErrors(Result result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(NormalizeField(error.Field), error.Message);
        }
    }

    protected void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(NormalizeField(error.PropertyName), error.ErrorMessage);
        }
    }

    protected static string NormalizeField(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(field[0]) + field[1..];
    }
}
