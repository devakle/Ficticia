using FluentValidation;
using Modules.People.Application.People.Commands;

namespace Modules.People.Application.Validators;

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IdentificationNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Age).InclusiveBetween(0, 120);
        RuleFor(x => x.Gender).InclusiveBetween(0, 3);
    }
}
