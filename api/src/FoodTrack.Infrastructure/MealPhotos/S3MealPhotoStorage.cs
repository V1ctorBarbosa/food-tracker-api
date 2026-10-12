using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using FoodTrack.Application.MealEstimates;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;
using Microsoft.Extensions.Options;

namespace FoodTrack.Infrastructure.MealPhotos;

public sealed class S3MealPhotoStorage : IMealPhotoStorage
{
    /// <summary>The app resizes photos to 1024 px JPEG, well under this; anything above it is not one of ours.</summary>
    public const long MaximumPhotoBytes = 5 * 1024 * 1024;

    private const string ContentTypeHeader = "Content-Type";

    // S3 deletes at most this many keys per DeleteObjects call.
    private const int MaximumKeysPerDelete = 1_000;

    private static readonly TimeSpan UploadSlotValidity = TimeSpan.FromMinutes(5);

    // Long enough for the estimator to fetch the photo, even after a retry.
    private static readonly TimeSpan PendingPhotoReadValidity = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan AttachedPhotoReadValidity = TimeSpan.FromHours(1);

    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3MealPhotoStorage(IAmazonS3 s3Client, IOptions<MealPhotoStorageOptions> options)
    {
        _s3Client = s3Client;
        _bucketName = options.Value.BucketName;
    }

    public async Task<PhotoUploadSlot> CreateUploadSlotAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(UploadSlotValidity);
        var uploadUrl = await _s3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = MealPhotoKeys.Pending(userId, captureId),
            Verb = HttpVerb.PUT,
            ContentType = MealPhotoKeys.PhotoContentType,
            Expires = expiresAt.UtcDateTime,
        });

        var headers = new Dictionary<string, string> { [ContentTypeHeader] = MealPhotoKeys.PhotoContentType };
        return new PhotoUploadSlot(new Uri(uploadUrl), headers, expiresAt);
    }

    /// <summary>
    /// A presigned upload cannot cap the body size, so the size is checked here instead, before
    /// the photo is sent to the estimator or kept on a meal.
    /// </summary>
    public async Task<StoredPhotoState> GetStateAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default)
    {
        if (await FindObjectSizeAsync(MealPhotoKeys.Attached(userId, captureId), cancellationToken) is not null)
        {
            return StoredPhotoState.Attached;
        }

        if (await FindObjectSizeAsync(MealPhotoKeys.Pending(userId, captureId), cancellationToken) is not { } pendingPhotoSize)
        {
            return StoredPhotoState.Missing;
        }

        if (pendingPhotoSize > MaximumPhotoBytes)
        {
            return StoredPhotoState.TooLarge;
        }

        return StoredPhotoState.Pending;
    }

    public Task<Uri> GetPendingPhotoReadUrlAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default) =>
        CreateReadUrlAsync(MealPhotoKeys.Pending(userId, captureId), PendingPhotoReadValidity);

    public Task<Uri> GetAttachedPhotoReadUrlAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default) =>
        CreateReadUrlAsync(MealPhotoKeys.Attached(userId, captureId), AttachedPhotoReadValidity);

    /// <summary>Moves the photo out of the pending prefix, so the lifecycle rule no longer expires it.</summary>
    public async Task MarkAttachedAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default)
    {
        var pendingKey = MealPhotoKeys.Pending(userId, captureId);
        try
        {
            await _s3Client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = _bucketName,
                SourceKey = pendingKey,
                DestinationBucket = _bucketName,
                DestinationKey = MealPhotoKeys.Attached(userId, captureId),
            }, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        await _s3Client.DeleteObjectAsync(_bucketName, pendingKey, cancellationToken);
    }

    public Task DeleteAsync(UserId userId, CaptureId captureId, CancellationToken cancellationToken = default) =>
        DeleteKeysAsync([MealPhotoKeys.Pending(userId, captureId), MealPhotoKeys.Attached(userId, captureId)], cancellationToken);

    public async Task DeleteAllAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        foreach (var prefix in MealPhotoKeys.UserPrefixes(userId))
        {
            var keys = await ListKeysAsync(prefix, cancellationToken);
            foreach (var chunk in keys.Chunk(MaximumKeysPerDelete))
            {
                await DeleteKeysAsync(chunk, cancellationToken);
            }
        }
    }

    private async Task<long?> FindObjectSizeAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _s3Client.GetObjectMetadataAsync(_bucketName, key, cancellationToken);
            return metadata.ContentLength;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<Uri> CreateReadUrlAsync(string key, TimeSpan validity)
    {
        var readUrl = await _s3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(validity),
        });

        return new Uri(readUrl);
    }

    private async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken)
    {
        var keys = new List<string>();
        var request = new ListObjectsV2Request { BucketName = _bucketName, Prefix = prefix };

        ListObjectsV2Response response;
        do
        {
            response = await _s3Client.ListObjectsV2Async(request, cancellationToken);
            keys.AddRange((response.S3Objects ?? []).Select(storedObject => storedObject.Key));
            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated == true);

        return keys;
    }

    private async Task DeleteKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return;
        }

        await _s3Client.DeleteObjectsAsync(new DeleteObjectsRequest
        {
            BucketName = _bucketName,
            Objects = keys.Select(key => new KeyVersion { Key = key }).ToList(),
        }, cancellationToken);
    }
}
