using System.Text.Json;
using FluentAssertions;
using FoodTrack.Infrastructure.MealEstimates;

namespace FoodTrack.Tests.Infrastructure.MealEstimates;

public sealed class MealEstimatePromptTest
{
    [Fact]
    public void BuildAnswerSchema_RequiresEveryFoodFieldTheParserReads()
    {
        // Act
        var schema = MealEstimatePrompt.BuildAnswerSchema();

        // Assert
        var foodSchema = schema["properties"].GetProperty(MealEstimatePrompt.FoodsProperty).GetProperty("items");
        var requiredFields = foodSchema.GetProperty("required").EnumerateArray().Select(field => field.GetString());
        requiredFields.Should().BeEquivalentTo(
            MealEstimatePrompt.NameProperty,
            MealEstimatePrompt.QuantityProperty,
            MealEstimatePrompt.BaseUnitProperty,
            MealEstimatePrompt.CaloriesProperty,
            MealEstimatePrompt.ProteinProperty,
            MealEstimatePrompt.CarbsProperty,
            MealEstimatePrompt.FatProperty);
        foodSchema.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
        schema["additionalProperties"].GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void BuildAnswerSchema_LimitsTheBaseUnitToGramAndMilliliter()
    {
        // Act
        var schema = MealEstimatePrompt.BuildAnswerSchema();

        // Assert
        var baseUnitValues = schema["properties"]
            .GetProperty(MealEstimatePrompt.FoodsProperty)
            .GetProperty("items")
            .GetProperty("properties")
            .GetProperty(MealEstimatePrompt.BaseUnitProperty)
            .GetProperty("enum")
            .EnumerateArray()
            .Select(value => value.GetString());
        baseUnitValues.Should().BeEquivalentTo("Gram", "Milliliter");
    }

    [Fact]
    public void BuildAnswerSchema_SerializesAsValidJson()
    {
        // Act
        var serializedSchema = JsonSerializer.Serialize(MealEstimatePrompt.BuildAnswerSchema());

        // Assert
        var act = () => JsonDocument.Parse(serializedSchema);
        act.Should().NotThrow();
    }

    [Fact]
    public void BuildDescriptionText_WrapsTheDescription()
    {
        // Act
        var text = MealEstimatePrompt.BuildDescriptionText("o frango é empanado");

        // Assert
        text.Should().Contain("<description>\no frango é empanado\n</description>");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildDescriptionText_SaysThereIsNone_WhenTheDescriptionIsBlank(string? description)
    {
        // Act
        var text = MealEstimatePrompt.BuildDescriptionText(description);

        // Assert
        text.Should().NotContain("<description>");
        text.Should().Contain("photo alone");
    }
}
