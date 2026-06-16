using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Authentication.DTOs;

namespace Osiris.Application.Features.Authentication.Commands.RegisterUserApi;

public sealed record RegisterUserApiCommand(
    string TenantName,
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword) : IRequest<Result<AuthTokensDto>>;
