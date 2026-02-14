using System.Text.Json;

namespace Modules.People.Application.Attributes.Validation;

public static class ValidationRulesParser
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AttributeValidationRules? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<AttributeValidationRules>(json, Opts); }
        catch { return null; }
    }
}
