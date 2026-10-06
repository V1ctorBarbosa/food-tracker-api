using Google.Cloud.Firestore;

namespace FoodTrack.Infrastructure.Firebase;

/// <summary>
/// Deletes many documents at once. The .NET Firestore client has no recursive delete, and a single
/// batch commits at most <see cref="MaximumWritesPerBatch"/> writes.
/// </summary>
internal static class FirestoreBulkDeletion
{
    public const int MaximumWritesPerBatch = 500;

    /// <summary>
    /// Deletes every document the query matches together with its <paramref name="subcollectionName"/>
    /// documents. Children are deleted before their parents, so if a batch fails the parents are still
    /// there for a retry to find the remaining children through.
    /// </summary>
    public static async Task DeleteWithSubcollectionAsync(
        FirestoreDb firestoreDb,
        Query query,
        string subcollectionName,
        CancellationToken cancellationToken)
    {
        var parentSnapshot = await query.GetSnapshotAsync(cancellationToken);
        var childSnapshots = await Task.WhenAll(parentSnapshot.Documents.Select(parent =>
            parent.Reference.Collection(subcollectionName).GetSnapshotAsync(cancellationToken)));

        var childReferences = childSnapshots.SelectMany(childSnapshot => childSnapshot.Documents).Select(child => child.Reference);
        var parentReferences = parentSnapshot.Documents.Select(parent => parent.Reference);

        await DeleteInBatchesAsync(firestoreDb, childReferences.Concat(parentReferences).ToList(), cancellationToken);
    }

    public static async Task DeleteMatchingAsync(FirestoreDb firestoreDb, Query query, CancellationToken cancellationToken)
    {
        var snapshot = await query.GetSnapshotAsync(cancellationToken);
        var references = snapshot.Documents.Select(document => document.Reference).ToList();

        await DeleteInBatchesAsync(firestoreDb, references, cancellationToken);
    }

    private static async Task DeleteInBatchesAsync(
        FirestoreDb firestoreDb,
        IReadOnlyList<DocumentReference> references,
        CancellationToken cancellationToken)
    {
        foreach (var batchReferences in references.Chunk(MaximumWritesPerBatch))
        {
            var batch = firestoreDb.StartBatch();
            foreach (var reference in batchReferences)
            {
                batch.Delete(reference);
            }

            await batch.CommitAsync(cancellationToken);
        }
    }
}
