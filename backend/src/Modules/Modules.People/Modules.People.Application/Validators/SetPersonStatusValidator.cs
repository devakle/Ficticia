using FluentValidation;
using Modules.People.Application.People.Commands;

namespace Modules.People.Application.Validators;

public sealed class SetPersonStatusValidator : AbstractValidator<SetPersonStatusCommand>
{
    public SetPersonStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
