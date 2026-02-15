namespace Modules.People.Application.Abstractions;

public static class PeopleErrors
{
    public const string NotFound = "people.not_found";
    public const string DuplicateIdentification = "people.duplicate_identification";
    public const string AttributeKeyInvalid = "attributes.invalid_key";
    public const string InvalidFilter = "filters.invalid";
    public const string UnsupportedType = "attributes.unsupported_type";
}
