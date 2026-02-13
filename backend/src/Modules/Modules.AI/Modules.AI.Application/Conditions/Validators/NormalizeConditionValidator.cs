using FluentValidation;
using Modules.AI.Application.Conditions.Commands;

namespace Modules.AI.Application.Conditions.Validators;

public sealed class NormalizeConditionValidator : AbstractValidator<NormalizeConditionCommand>
{
    public NormalizeConditionValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(500);
    }
}
