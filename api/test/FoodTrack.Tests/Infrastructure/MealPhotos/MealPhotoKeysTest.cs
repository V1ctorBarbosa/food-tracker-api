using FluentAssertions;
using FoodTrack.Domain.Meals;
using FoodTrack.Domain.Users;
using FoodTrack.Infrastructure.MealPhotos;

namespace FoodTrack.Tests.Infrastructure.MealPhotos;

public sealed class MealPhotoKeysTest
{
    private static readonly UserId UserId = new("firebase-uid-123");
    private static readonly CaptureId CaptureId = new("6f9619ff-8b86-d011-b42d-00c04fc964ff");

    [Fact]
    public void Pending_PutsThePhotoUnderThePendingPrefix()
    {
        // Act
        var key = MealPhotoKeys.Pending(UserId, CaptureId);

        // Assert
        key.Should().Be("pending/firebase-uid-123/6f9619ff-8b86-d011-b42d-00c04fc964ff.jpg");
        key.Should().StartWith(MealPhotoKeys.PendingPrefix);
    }

    [Fact]
    public void Attached_PutsThePhotoUnderTheAttachedPrefix()
    {
        // Act
        var key = MealPhotoKeys.Attached(UserId, CaptureId);

        // Assert
        key.Should().Be("attached/firebase-uid-123/6f9619ff-8b86-d011-b42d-00c04fc964ff.jpg");
    }

    [Fact]
    public void UserPrefixes_CoverBothThePendingAndTheAttachedPhotosOfTheUser()
    {
        // Act
        var prefixes = MealPhotoKeys.UserPrefixes(UserId);

        // Assert
        prefixes.Should().Equal("pending/firebase-uid-123/", "attached/firebase-uid-123/");
        MealPhotoKeys.Pending(UserId, CaptureId).Should().StartWith(prefixes[0]);
        MealPhotoKeys.Attached(UserId, CaptureId).Should().StartWith(prefixes[1]);
    }

    [Fact]
    public void UserPrefixes_DoNotCoverAUserWhoseIdStartsTheSame()
    {
        // Arrange
        var otherUserId = new UserId("firebase-uid-1234");

        // Act
        var prefixes = MealPhotoKeys.UserPrefixes(UserId);

        // Assert
        MealPhotoKeys.Pending(otherUserId, CaptureId).Should().NotStartWith(prefixes[0]);
    }

    [Fact]
    public void Pending_Throws_WhenTheUserIdWouldReachAnotherFolder()
    {
        // Act
        var act = () => MealPhotoKeys.Pending(new UserId("other-user/../firebase-uid-123"), CaptureId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
