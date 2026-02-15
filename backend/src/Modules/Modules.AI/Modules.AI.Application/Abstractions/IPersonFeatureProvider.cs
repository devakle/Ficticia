namespace Modules.AI.Application.Abstractions;

public interface IPersonFeatureProvider
{
    Task<PersonRiskFeatures> GetRiskFeaturesAsync(Guid personId, CancellationToken ct);
}

public sealed record PersonRiskFeatures(
    int Age,
    int Gender, // o string
    string? ConditionCode,
    bool? Diabetic,
    bool? Smoker
);
