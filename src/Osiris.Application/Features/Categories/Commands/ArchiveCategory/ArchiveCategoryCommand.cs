using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Categories.Commands.ArchiveCategory;

public sealed record ArchiveCategoryCommand(Guid Id) : IRequest<Result>;
