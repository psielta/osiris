using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Application.Features.Authentication.Commands.ForgotPassword;
using Osiris.Application.Features.Authentication.Commands.LoginUser;
using Osiris.Application.Features.Authentication.Commands.LogoutUser;
using Osiris.Application.Features.Authentication.Commands.RegisterUser;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

public sealed class AccountController : AppController
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new LoginUserCommand(model.Email, model.Password, model.RememberMe),
                cancellationToken);

            if (result.IsFailure)
            {
                AddResultErrors(result);
                return View(model);
            }

            return RedirectAfterSignIn(model.ReturnUrl);
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new RegisterUserCommand(
                    model.TenantName,
                    model.FullName,
                    model.Email,
                    model.Password,
                    model.ConfirmPassword),
                cancellationToken);

            if (result.IsFailure)
            {
                AddResultErrors(result);
                return View(model);
            }

            return RedirectToAction("Index", "Dashboard");
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _mediator.Send(new LogoutUserCommand(), cancellationToken);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new ForgotPasswordCommand(model.Email), cancellationToken);
            ViewData["SuccessMessage"] = "Se a conta existir, as instruções de recuperação serão enviadas.";
            return View(new ForgotPasswordViewModel());
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectAfterSignIn(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

}
