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

    [Fact]
    public void Required_boolean_should_pass_when_value_present()
    {
        var rules = new AttributeValidationRules { Required = true };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "diabetic",
            dataType: AttributeDataType.Boolean,
            rules: rules,
            boolValue: false,
            stringValue: null,
            numberValue: null,
            dateValue: null);

        ok.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Enum_should_ignore_validation_when_value_is_null()
    {
        var rules = new AttributeValidationRules
        {
            AllowedValues = new[] { "diabetes" }
        };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "condition_code",
            dataType: AttributeDataType.Enum,
            rules: rules,
            boolValue: null,
            stringValue: null,
            numberValue: null,
            dateValue: null);

        ok.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Enum_should_trim_spaces_before_comparing_allowed_values()
    {
        var rules = new AttributeValidationRules
        {
            AllowedValues = new[] { " diabetes ", "asma" }
        };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "condition_code",
            dataType: AttributeDataType.Enum,
            rules: rules,
            boolValue: null,
            stringValue: "  Diabetes ",
            numberValue: null,
            dateValue: null);

        ok.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void String_should_fail_when_exceeds_max_length()
    {
        var rules = new AttributeValidationRules { MaxLength = 5 };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "disease_text",
            dataType: AttributeDataType.String,
            rules: rules,
            boolValue: null,
            stringValue: "123456",
            numberValue: null,
            dateValue: null);

        ok.Should().BeFalse();
        error.Should().Contain("maxLength=5");
    }

    [Fact]
    public void String_should_pass_when_matches_regex()
    {
        var rules = new AttributeValidationRules { Regex = "^[A-Z]{3}$" };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "code",
            dataType: AttributeDataType.String,
            rules: rules,
            boolValue: null,
            stringValue: "ABC",
            numberValue: null,
            dateValue: null);

        ok.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void String_should_fail_when_does_not_match_regex()
    {
        var rules = new AttributeValidationRules { Regex = "^[A-Z]{3}$" };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "code",
            dataType: AttributeDataType.String,
            rules: rules,
            boolValue: null,
            stringValue: "abc",
            numberValue: null,
            dateValue: null);

        ok.Should().BeFalse();
        error.Should().Contain("formato inválido");
    }

    [Fact]
    public void String_should_ignore_invalid_regex_configuration()
    {
        var rules = new AttributeValidationRules { Regex = "([a-z" };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "code",
            dataType: AttributeDataType.String,
            rules: rules,
            boolValue: null,
            stringValue: "anything",
            numberValue: null,
            dateValue: null);

        ok.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Number_should_fail_when_below_min()
    {
        var rules = new AttributeValidationRules { Min = 10 };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "weight",
            dataType: AttributeDataType.Number,
            rules: rules,
            boolValue: null,
            stringValue: null,
            numberValue: 9.99m,
            dateValue: null);

        ok.Should().BeFalse();
        error.Should().Contain(">= 10");
    }

    [Fact]
    public void Number_should_fail_when_above_max()
    {
        var rules = new AttributeValidationRules { Max = 100 };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "weight",
            dataType: AttributeDataType.Number,
            rules: rules,
            boolValue: null,
            stringValue: null,
            numberValue: 100.01m,
            dateValue: null);

        ok.Should().BeFalse();
        error.Should().Contain("<= 100");
    }

    [Fact]
    public void Number_should_pass_on_boundaries()
    {
        var rules = new AttributeValidationRules { Min = 10, Max = 100 };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "weight",
            dataType: AttributeDataType.Number,
            rules: rules,
            boolValue: null,
            stringValue: null,
            numberValue: 10m,
            dateValue: null);

        ok.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Date_should_fail_when_before_min_date()
    {
        var rules = new AttributeValidationRules { MinDate = new DateTime(2020, 01, 01) };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "onset",
            dataType: AttributeDataType.Date,
            rules: rules,
            boolValue: null,
            stringValue: null,
            numberValue: null,
            dateValue: new DateTime(2019, 12, 31, 23, 59, 59));

        ok.Should().BeFalse();
        error.Should().Contain(">= 2020-01-01");
    }

    [Fact]
    public void Date_should_fail_when_after_max_date()
    {
        var rules = new AttributeValidationRules { MaxDate = new DateTime(2020, 12, 31) };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "onset",
            dataType: AttributeDataType.Date,
            rules: rules,
            boolValue: null,
            stringValue: null,
            numberValue: null,
            dateValue: new DateTime(2021, 01, 01, 00, 00, 01));

        ok.Should().BeFalse();
        error.Should().Contain("<= 2020-12-31");
    }

    [Fact]
    public void Date_should_compare_only_date_part()
    {
        var rules = new AttributeValidationRules
        {
            MinDate = new DateTime(2020, 01, 01),
            MaxDate = new DateTime(2020, 01, 01)
        };

        var (ok, error) = AttributeRulesValidator.Validate(
            key: "onset",
            dataType: AttributeDataType.Date,
            rules: rules,
            boolValue: null,
            stringValue: null,
            numberValue: null,
            dateValue: new DateTime(2020, 01, 01, 23, 59, 59));

        ok.Should().BeTrue();
        error.Should().BeNull();
    }
}
