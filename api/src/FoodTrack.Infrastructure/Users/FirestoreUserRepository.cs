using FoodTrack.Application.Users;
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

        return UserDocumentMapper.ToDomain(userId, snapshot.ConvertTo<UserDocument>());
    }

    public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        var document = UserDocumentMapper.ToDocument(user);

        await _firestoreDb.Collection(CollectionName)
            .Document(user.Id.Value)
            .SetAsync(document, cancellationToken: cancellationToken);
    }
}
