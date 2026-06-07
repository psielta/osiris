using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
