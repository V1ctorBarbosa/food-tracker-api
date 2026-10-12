using FluentAssertions;
using FoodTrack.Application.MealEstimates;
using FoodTrack.Infrastructure.MealEstimates;

namespace FoodTrack.Tests.Infrastructure.MealEstimates;

public sealed class RecognizedMealJsonParserTest
{
    [Fact]
    public void Parse_ReadsEveryFood()
    {
        // Arrange
        const string answerJson = """
            {
              "foods": [
                { "name": "Arroz branco cozido", "quantity": 200, "baseUnit": "Gram", "caloriesPer100": 128, "proteinPer100": 2.5, "carbsPer100": 28.1, "fatPer100": 0.2 },
                { "name": "Suco de laranja", "quantity": 300, "baseUnit": "Milliliter", "caloriesPer100": 45, "proteinPer100": 0.7, "carbsPer100": 10.4, "fatPer100": 0.2 }
              ]
            }
            """;

        // Act
        var recognizedMeal = RecognizedMealJsonParser.Parse(answerJson);

        // Assert
        recognizedMeal.Foods.Should().Equal(
            new RecognizedFood("Arroz branco cozido", 200, "Gram", 128, 2.5, 28.1, 0.2),
            new RecognizedFood("Suco de laranja", 300, "Milliliter", 45, 0.7, 10.4, 0.2));
    }

    [Fact]
    public void Parse_ReturnsNoFoods_ForAnEmptyList()
    {
        // Act
        var recognizedMeal = RecognizedMealJsonParser.Parse("""{ "foods": [] }""");

        // Assert
        recognizedMeal.Foods.Should().BeEmpty();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{ "foods": [ { "name": "Arroz" } ] }""")]
    [InlineData("""{ "foods": [ { "name": "Arroz", "quantity": "a lot", "baseUnit": "Gram", "caloriesPer100": 128, "proteinPer100": 2.5, "carbsPer100": 28.1, "fatPer100": 0.2 } ] }""")]
    [InlineData("""{ "foods": "rice" }""")]
    public void Parse_Throws_WhenTheAnswerHasAnotherShape(string answerJson)
    {
        // Act
        var act = () => RecognizedMealJsonParser.Parse(answerJson);

        // Assert
        act.Should().Throw<MealEstimatorUnavailableException>();
    }
}
