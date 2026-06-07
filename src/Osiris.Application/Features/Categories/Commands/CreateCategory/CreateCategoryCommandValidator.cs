using FluentValidation;

namespace Osiris.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("Informe o nome da categoria.")
            .MaximumLength(100)
            .WithMessage("O nome da categoria deve ter no máximo 100 caracteres.");

        RuleFor(command => command.Type)
            .NotNull()
            .WithMessage("Selecione o tipo da categoria.")
            .IsInEnum()
            .WithMessage("Selecione um tipo de categoria válido.");

        RuleFor(command => command.Color)
            .MaximumLength(7)
            .WithMessage("A cor deve ter no máximo 7 caracteres.")
            .Matches("^#[0-9A-Fa-f]{6}$")
            .When(command => !string.IsNullOrWhiteSpace(command.Color))
            .WithMessage("Escolha uma cor válida.");
    }
}
