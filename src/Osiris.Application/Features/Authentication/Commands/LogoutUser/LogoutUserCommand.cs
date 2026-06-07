using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.LogoutUser;

public sealed record LogoutUserCommand : IRequest<Result>;
