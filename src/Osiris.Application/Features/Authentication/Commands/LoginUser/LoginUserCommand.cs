using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.LoginUser;

public sealed record LoginUserCommand(
    string Email,
    string Password,
    bool RememberMe) : IRequest<Result>;
