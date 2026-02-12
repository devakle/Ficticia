using Modules.People.Domain.Enums;

namespace Modules.People.Domain.Entities;

public sealed class AttributeDefinition
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Key { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public AttributeDataType DataType { get; private set; }

    public bool IsFilterable { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    public string? ValidationRulesJson { get; private set; }

    private AttributeDefinition() { }

    public AttributeDefinition(string key, string displayName, AttributeDataType dataType, bool isFilterable, string? rulesJson)
    {
        Key = NormalizeKey(key);
        DisplayName = (displayName ?? "").Trim();
        DataType = dataType;
        IsFilterable = isFilterable;
        ValidationRulesJson = rulesJson;
        IsActive = true;

        if (DisplayName.Length == 0) throw new ArgumentException("DisplayName requerido");
    }

    public void Update(string displayName, bool isFilterable, string? rulesJson, bool isActive)
    {
        DisplayName = (displayName ?? "").Trim();
        if (DisplayName.Length == 0) throw new ArgumentException("DisplayName requerido");

        IsFilterable = isFilterable;
        ValidationRulesJson = rulesJson;
        IsActive = isActive;
    }

    public static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key requerido");
        return key.Trim().ToLowerInvariant();
    }
}
