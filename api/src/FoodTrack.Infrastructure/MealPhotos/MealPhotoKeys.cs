using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;

namespace FoodTrack.Infrastructure.MealPhotos;

/// <summary>
/// Where each photo lives in the bucket. Pending and attached photos sit under separate top-level
/// prefixes because a bucket lifecycle rule can only match a key prefix: the rule expiring
/// unconfirmed uploads covers <see cref="PendingPrefix"/> and nothing else. Every key is built
/// here from the signed-in user and a <see cref="CaptureId"/>, never from client text.
/// </summary>
internal static class MealPhotoKeys
{
    public const string PendingPrefix = "pending/";
    public const string AttachedPrefix = "attached/";
    public const string PhotoContentType = "image/jpeg";

    private const string PhotoExtension = ".jpg";
    private const char KeySeparator = '/';

    public static string Pending(UserId userId, CaptureId captureId) => $"{PendingPrefix}{ToKeySegment(userId)}/{captureId.Value}{PhotoExtension}";

    public static string Attached(UserId userId, CaptureId captureId) => $"{AttachedPrefix}{ToKeySegment(userId)}/{captureId.Value}{PhotoExtension}";

    public static IReadOnlyList<string> UserPrefixes(UserId userId) =>
    [
        $"{PendingPrefix}{ToKeySegment(userId)}/",
        $"{AttachedPrefix}{ToKeySegment(userId)}/",
    ];

    /// <summary>
    /// A user id is one key segment. Firebase ids never contain a separator, but one that did
    /// would reach into another user's folder, so it is refused rather than trusted.
    /// </summary>
    private static string ToKeySegment(UserId userId)
    {
        if (userId.Value.Contains(KeySeparator))
        {
            throw new ArgumentException("User id cannot be used in a storage key.", nameof(userId));
        }

        return userId.Value;
    }
}
