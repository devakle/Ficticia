using FluentAssertions;
using Modules.People.Application.Attributes.Validation;
using Modules.People.Domain.Enums;
using Xunit;

public sealed class AttributeValueShapeValidatorTests
{
    [Fact]
    public void Should_fail_when_more_than_one_value_is_provided()
    {
        var (ok, error) = AttributeValueShapeValidator.ValidateShape(
            key: "condition_code",
            dataType: AttributeDataType.Enum,
            boolValue: true,
            stringValue: "hypertension",
            numberValue: null,
            dateValue: null);

        ok.Should().BeFalse();
        error.Should().Contain("solo un tipo");
    }

    [Fact]
    public void Should_fail_when_value_type_does_not_match_datatype()
    {
        var (ok, error) = AttributeValueShapeValidator.ValidateShape(
            key: "age_extra",
            dataType: AttributeDataType.Number,
            boolValue: null,
            stringValue: "123",
            numberValue: null,
            dateValue: null);

        ok.Should().BeFalse();
        error.Should().Contain("no coincide");
    }

    [Fact]
    public void Should_allow_empty_to_clear_value()
    {
        var (ok, error) = AttributeValueShapeValidator.ValidateShape(
            key: "diabetic",
            dataType: AttributeDataType.Boolean,
            boolValue: null,
            stringValue: null,
            numberValue: null,
            dateValue: null);

        ok.Should().BeTrue();
        error.Should().BeNull();
    }
}
