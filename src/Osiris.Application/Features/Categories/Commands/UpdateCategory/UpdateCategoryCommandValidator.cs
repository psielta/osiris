using FluentValidation;

namespace Osiris.Application.Features.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Type)
            .NotNull()
            .IsInEnum();

        RuleFor(command => command.Color)
            .MaximumLength(7)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .When(command => !string.IsNullOrWhiteSpace(command.Color))
            .WithMessage("Color must be a hex color such as #A1B2C3.");
    }
}
