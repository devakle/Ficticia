namespace Modules.People.Application.Attributes.Validation;

public sealed class AttributeValidationRules
{
    public bool? Required { get; init; }

    // string
    public int? MaxLength { get; init; }
    public string? Regex { get; init; }

    // number
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }

    // date
    public DateTime? MinDate { get; init; }
    public DateTime? MaxDate { get; init; }

    // enum
    public IReadOnlyList<string>? AllowedValues { get; init; }
}
