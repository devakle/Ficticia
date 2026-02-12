using FluentValidation;
using Modules.People.Application.Attributes.Commands;

namespace Modules.People.Application.Validators;

public sealed class CreateAttributeDefinitionValidator : AbstractValidator<CreateAttributeDefinitionCommand>
{
    public CreateAttributeDefinitionValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DataType).InclusiveBetween(1, 5);
    }
}
