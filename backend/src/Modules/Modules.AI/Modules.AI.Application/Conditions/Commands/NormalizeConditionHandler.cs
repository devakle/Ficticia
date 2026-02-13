using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.AI.Application.Abstractions;
using Modules.AI.Application.Abstractions.Models;
using Modules.AI.Contracts.Dtos;

namespace Modules.AI.Application.Conditions.Commands;

internal sealed class NormalizeConditionHandler
    : IRequestHandler<NormalizeConditionCommand, Result<NormalizeConditionResponseDto>>
{
    private readonly IConditionNormalizer _normalizer;

    public NormalizeConditionHandler(IConditionNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public async Task<Result<NormalizeConditionResponseDto>> Handle(NormalizeConditionCommand req, CancellationToken ct)
    {
        try
        {
            NormalizedCondition n = await _normalizer.NormalizeAsync(req.Text, ct);

            return Result<NormalizeConditionResponseDto>.Ok(new NormalizeConditionResponseDto(
                n.Code,
                n.Label,
                n.Confidence,
                n.MatchedTerms,
                n.SuggestedAttributes,
                n.Source
            ));
        }
        catch (Exception ex)
        {
            return Result<NormalizeConditionResponseDto>.Fail(AiErrors.ProviderFailed, ex.Message);
        }
    }
}
