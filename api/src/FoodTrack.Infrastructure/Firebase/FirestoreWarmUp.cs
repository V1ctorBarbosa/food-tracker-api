using FoodTrack.Infrastructure.Startup;
using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Firebase;

/// <summary>
/// Reads one document that is not expected to exist so the Firestore channel is opened, the
/// credential is exercised and the client code is compiled before the first user request needs them.
/// </summary>
public sealed class FirestoreWarmUp : IStartupWarmUp
{
    private const string WarmUpCollectionName = "_health";
    private const string WarmUpDocumentId = "ping";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreWarmUp(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public string Name => "Firestore";

    public async Task WarmUpAsync(CancellationToken cancellationToken)
    {
        await _firestoreDb.Collection(WarmUpCollectionName).Document(WarmUpDocumentId).GetSnapshotAsync(cancellationToken);
    }
}
