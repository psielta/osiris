using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string TenantName,
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword) : IRequest<Result>;
