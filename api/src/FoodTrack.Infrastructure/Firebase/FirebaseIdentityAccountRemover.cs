using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FoodTrack.Application.Users;
using FoodTrack.Domain.Users;

namespace FoodTrack.Infrastructure.Firebase;

public sealed class FirebaseIdentityAccountRemover : IIdentityAccountRemover
{
    private readonly FirebaseAuth _firebaseAuth;

    public FirebaseIdentityAccountRemover(FirebaseApp firebaseApp)
    {
        _firebaseAuth = FirebaseAuth.GetAuth(firebaseApp);
    }

    public async Task RemoveAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _firebaseAuth.DeleteUserAsync(userId.Value, cancellationToken);
        }
        catch (FirebaseAuthException exception) when (exception.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            // Already gone, e.g. a retry after an earlier attempt removed it.
        }
    }
}
