using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.RevokeRefreshToken;

public sealed record RevokeRefreshTokenCommand(string RefreshToken) : IRequest<Result>;
