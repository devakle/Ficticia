using FluentValidation;
using Modules.People.Application.Attributes.Commands;

namespace Modules.People.Application.Validators;

public sealed class UpdateAttributeDefinitionValidator : AbstractValidator<UpdateAttributeDefinitionCommand>
{
    public UpdateAttributeDefinitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}
