namespace FoodTrack.Domain.Users;

/// <summary>
/// Wraps the authenticated Firebase Auth UID. A User's identity always originates
/// from a verified token — never from client-supplied input.
/// </summary>
public readonly record struct UserId
{
    public string Value { get; }

    public UserId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("User id cannot be empty.", nameof(value));
        }

        Value = value;
    }
}
