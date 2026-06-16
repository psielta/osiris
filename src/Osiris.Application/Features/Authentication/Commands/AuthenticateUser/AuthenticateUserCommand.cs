using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.DTOs;

namespace Osiris.Application.Features.Authentication.Commands.AuthenticateUser;

public sealed record AuthenticateUserCommand(string Email, string Password) : IRequest<Result<AuthTokensDto>>;
