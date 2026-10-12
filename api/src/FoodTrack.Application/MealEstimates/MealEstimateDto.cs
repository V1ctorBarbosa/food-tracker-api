namespace FoodTrack.Application.MealEstimates;

/// <param name="CaptureId">Send it back as the capture of the meal the items are saved in.</param>
public sealed record MealEstimateDto(string CaptureId, bool HasPhoto, string? Description, IReadOnlyList<EstimatedMealItemDto> Items);
