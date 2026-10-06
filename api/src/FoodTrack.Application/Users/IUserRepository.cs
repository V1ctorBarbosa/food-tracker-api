using FoodTrack.Domain.Users;

namespace FoodTrack.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);

    Task SaveAsync(User user, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserId userId, CancellationToken cancellationToken = default);
}
