namespace Modules.AI.Infrastructure.Risk;

public sealed class RiskRulesOptions
{
    public int BaseScore { get; init; } = 10;

    public int Age_18_30 { get; init; } = 5;
    public int Age_31_45 { get; init; } = 15;
    public int Age_46_60 { get; init; } = 30;
    public int Age_61_plus { get; init; } = 45;

    public int Smoker { get; init; } = 20;
    public int Hypertension { get; init; } = 15;
    public int Diabetes { get; init; } = 25;
    public int HeartDisease { get; init; } = 35;

    public int LowMax { get; init; } = 39;
    public int MediumMax { get; init; } = 69;
}
