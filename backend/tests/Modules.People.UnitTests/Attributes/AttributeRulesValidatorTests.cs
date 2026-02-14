using FluentAssertions;
using Modules.People.Application.Attributes.Validation;
using Modules.People.Domain.Enums;
using Xunit;

public sealed class AttributeRulesValidatorTests
{
    [Fact]
    public void Enum_should_reject_value_outside_allowed_values()
    {
        var rules = new AttributeValidationRules
        {
            AllowedValues = new[] { "hypertension", "diabetes", "unknown" }
        };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "condition_code",
            dataType: AttributeDataType.Enum,
            rules: rules,
            boolValue: null,
            stringValue: "anything",
            numberValue: null,
            dateValue: null);

        ok.Should().BeFalse();
        error.Should().Contain("debe ser uno de");
    }

    [Fact]
    public void Enum_should_accept_value_in_allowed_values_case_insensitive()
    {
        var rules = new AttributeValidationRules
        {
            AllowedValues = new[] { "hypertension", "diabetes" }
        };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "condition_code",
            dataType: AttributeDataType.Enum,
            rules: rules,
            boolValue: null,
            stringValue: "HyPeRtEnSiOn",
            numberValue: null,
            dateValue: null);

        ok.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Required_should_fail_when_missing()
    {
        var rules = new AttributeValidationRules { Required = true };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "disease_text",
            dataType: AttributeDataType.String,
            rules: rules,
            boolValue: null,
            stringValue: null,
            numberValue: null,
            dateValue: null);

        ok.Should().BeFalse();
        error.Should().Contain("requerido");
    }
}
