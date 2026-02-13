namespace Modules.AI.Infrastructure.OpenAI;

public static class OpenAiSchemas
{
    // Lista cerrada: agregá los códigos que quieras soportar.
    public static readonly string[] AllowedConditionCodes =
    [
        "hypertension",
        "diabetes",
        "asthma",
        "heart_disease",
        "none",
        "unknown"
    ];

    public static object NormalizeConditionJsonSchema()
    {
        return new
        {
            name = "normalize_condition",
            strict = true,
            schema = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "code", "label", "confidence", "matched_terms" },
                properties = new
                {
                    code = new
                    {
                        type = "string",
                        @enum = AllowedConditionCodes
                    },
                    label = new { type = "string" },
                    confidence = new { type = "number", minimum = 0, maximum = 1 },
                    matched_terms = new
                    {
                        type = "array",
                        items = new { type = "string" }
                    }
                }
            }
        };
    }
}
