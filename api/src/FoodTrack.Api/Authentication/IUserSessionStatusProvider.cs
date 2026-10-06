namespace FoodTrack.Api.Authentication;

public interface IUserSessionStatusProvider
{
    Task<UserSessionStatus> GetAsync(string uid, CancellationToken cancellationToken = default);
}
