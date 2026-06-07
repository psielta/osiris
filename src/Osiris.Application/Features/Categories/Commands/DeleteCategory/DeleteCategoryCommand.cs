using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>;
