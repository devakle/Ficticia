using System.Text.RegularExpressions;
using Modules.People.Domain.Enums;

namespace Modules.People.Application.Attributes.Validation;

public static class AttributeRulesValidator
{
    public static (bool ok, string? error) Validate(
        string key,
        AttributeDataType dataType,
        AttributeValidationRules? rules,
        bool? boolValue,
        string? stringValue,
        decimal? numberValue,
        DateTime? dateValue)
    {
        // required
        if (rules?.Required == true)
        {
            var has = dataType switch
            {
                AttributeDataType.Boolean => boolValue is not null,
                AttributeDataType.String => !string.IsNullOrWhiteSpace(stringValue),
                AttributeDataType.Enum => !string.IsNullOrWhiteSpace(stringValue),
                AttributeDataType.Number => numberValue is not null,
                AttributeDataType.Date => dateValue is not null,
                _ => true
            };

            if (!has) return (false, $"'{key}': requerido");
        }

        switch (dataType)
        {
            case AttributeDataType.Enum:
                if (stringValue is null) return (true, null);

                if (rules?.AllowedValues is { Count: > 0 })
                {
                    var allowed = rules.AllowedValues
                        .Select(x => x.Trim().ToLowerInvariant())
                        .Where(x => x.Length > 0)
                        .ToHashSet();

                    var v = stringValue.Trim().ToLowerInvariant();
                    if (!allowed.Contains(v))
                        return (false, $"'{key}': debe ser uno de [{string.Join(", ", allowed)}]");
                }
                return (true, null);

            case AttributeDataType.String:
                if (stringValue is null) return (true, null);

                if (rules?.MaxLength is not null && stringValue.Length > rules.MaxLength.Value)
                    return (false, $"'{key}': supera maxLength={rules.MaxLength}");

                if (!string.IsNullOrWhiteSpace(rules?.Regex))
                {
                    try
                    {
                        if (!Regex.IsMatch(stringValue, rules.Regex))
                            return (false, $"'{key}': formato inválido");
                    }
                    catch { /* regex mal configurada -> ignorar o fallar hard si querés */ }
                }
                return (true, null);

            case AttributeDataType.Number:
                if (numberValue is null) return (true, null);
                if (rules?.Min is not null && numberValue.Value < rules.Min.Value)
                    return (false, $"'{key}': debe ser >= {rules.Min}");
                if (rules?.Max is not null && numberValue.Value > rules.Max.Value)
                    return (false, $"'{key}': debe ser <= {rules.Max}");
                return (true, null);

            case AttributeDataType.Date:
                if (dateValue is null) return (true, null);
                var d = dateValue.Value.Date;
                if (rules?.MinDate is not null && d < rules.MinDate.Value.Date)
                    return (false, $"'{key}': debe ser >= {rules.MinDate:yyyy-MM-dd}");
                if (rules?.MaxDate is not null && d > rules.MaxDate.Value.Date)
                    return (false, $"'{key}': debe ser <= {rules.MaxDate:yyyy-MM-dd}");
                return (true, null);

            case AttributeDataType.Boolean:
            default:
                return (true, null);
        }
    }
}
