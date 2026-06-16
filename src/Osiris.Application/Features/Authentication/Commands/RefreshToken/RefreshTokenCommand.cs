using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.DTOs;

namespace Osiris.Application.Features.Authentication.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokensDto>>;
