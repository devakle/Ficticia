using Modules.People.Domain.Enums;

namespace Modules.People.Application.Attributes.Validation;

public static class AttributeValueShapeValidator
{
    public static (bool ok, string? error) ValidateShape(
        string key,
        AttributeDataType dataType,
        bool? boolValue,
        string? stringValue,
        decimal? numberValue,
        DateTime? dateValue)
    {
        var filled =
            (boolValue is not null ? 1 : 0) +
            (!string.IsNullOrWhiteSpace(stringValue) ? 1 : 0) +
            (numberValue is not null ? 1 : 0) +
            (dateValue is not null ? 1 : 0);

        // permitimos 0 (limpiar) o 1
        if (filled > 1) return (false, $"'{key}': enviar solo un tipo de valor");

        if (filled == 1)
        {
            var match = dataType switch
            {
                AttributeDataType.Boolean => boolValue is not null,
                AttributeDataType.String => !string.IsNullOrWhiteSpace(stringValue),
                AttributeDataType.Enum => !string.IsNullOrWhiteSpace(stringValue),
                AttributeDataType.Number => numberValue is not null,
                AttributeDataType.Date => dateValue is not null,
                _ => false
            };

            if (!match) return (false, $"'{key}': el valor no coincide con el tipo {dataType}");
        }

        return (true, null);
    }
}
