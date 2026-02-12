namespace Modules.People.Domain.Entities;

public sealed class PersonAttributeValue
{
    public Guid PersonId { get; private set; }
    public Guid AttributeDefinitionId { get; private set; }

    public bool? ValueBool { get; private set; }
    public string? ValueString { get; private set; }
    public decimal? ValueNumber { get; private set; }
    public DateTime? ValueDate { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private PersonAttributeValue() { }

    public PersonAttributeValue(Guid personId, Guid attributeDefinitionId)
    {
        PersonId = personId;
        AttributeDefinitionId = attributeDefinitionId;
    }

    public void SetBool(bool? value) { Clear(); ValueBool = value; Touch(); }
    public void SetString(string? value) { Clear(); ValueString = value?.Trim(); Touch(); }
    public void SetNumber(decimal? value) { Clear(); ValueNumber = value; Touch(); }
    public void SetDate(DateTime? value) { Clear(); ValueDate = value?.Date; Touch(); }

    private void Clear()
    {
        ValueBool = null;
        ValueString = null;
        ValueNumber = null;
        ValueDate = null;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
