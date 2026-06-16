using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserProfileDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(ICurrentUser currentUser, IIdentityService identityService)
    {
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<Result<UserProfileDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result<UserProfileDto>.Failure(
                new ResultError("Usuário não autenticado.", null, ResultErrorCodes.Unauthorized));
        }

        return await _identityService.GetProfileAsync(userId, cancellationToken);
    }
}
