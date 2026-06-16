using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<UserProfileDto>>;
