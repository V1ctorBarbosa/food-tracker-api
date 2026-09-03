using FoodTrack.Application.Users;
using FoodTrack.Domain.Common;
using FoodTrack.Domain.Users;
using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Users;

public sealed class FirestoreUserRepository : IUserRepository
{
    private const string CollectionName = "users";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreUserRepository(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _firestoreDb.Collection(CollectionName)
            .Document(userId.Value)
            .GetSnapshotAsync(cancellationToken);

        if (!snapshot.Exists)
        {
            return null;
        }

        return MapToDomain(userId, snapshot.ConvertTo<UserDocument>());
    }

    public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        var document = MapToDocument(user);

        await _firestoreDb.Collection(CollectionName)
            .Document(user.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }

    private static User MapToDomain(UserId id, UserDocument document) => User.Create(
        id,
        document.Name,
        new Age(document.Age),
        new Centimeters(document.HeightCentimeters),
        Enum.Parse<Sex>(document.Sex),
        Enum.Parse<ActivityLevel>(document.ActivityLevel),
        new Kilocalories(document.DailyCalorieTarget),
        new Kilocalories(document.DesiredCalorieDeficit),
        document.TargetWeightKilograms is { } weight ? new Kilograms(weight) : null);

    private static UserDocument MapToDocument(User user) => new()
    {
        Name = user.Name,
        Age = user.Age.Value,
        HeightCentimeters = user.Height.Value,
        Sex = user.Sex.ToString(),
        ActivityLevel = user.ActivityLevel.ToString(),
        TargetWeightKilograms = user.TargetWeight?.Value,
        DesiredCalorieDeficit = user.DesiredCalorieDeficit.Value,
        DailyCalorieTarget = user.DailyCalorieTarget.Value,
    };
}
