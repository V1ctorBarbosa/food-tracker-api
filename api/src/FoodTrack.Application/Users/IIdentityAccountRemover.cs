using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Users;

/// <summary>Removes the user's sign-in account from the identity provider.</summary>
public interface IIdentityAccountRemover
{
    /// <summary>Succeeds as well when the account no longer exists, so a retried deletion can finish.</summary>
    Task RemoveAsync(UserId userId, CancellationToken cancellationToken = default);
}
