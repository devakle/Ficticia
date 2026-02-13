using FluentValidation;
using Modules.AI.Application.PeopleRisk.Commands;

namespace Modules.AI.Application.PeopleRisk.Validators;

public sealed class ScorePersonRiskValidator : AbstractValidator<ScorePersonRiskCommand>
{
    public ScorePersonRiskValidator()
    {
        RuleFor(x => x.PersonId).NotEmpty();
    }
}
