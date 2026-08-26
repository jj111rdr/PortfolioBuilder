using Portfolio_Builder.Entities.Models;

namespace Portfolio_Builder.Services;

/// <summary>
/// Outcome of a create-user attempt. Either it succeeded (User is set),
/// or it failed with a specific reason (Error is set).
/// </summary>
public class CreateUserResult
{
    public bool Succeeded { get; init; }
    public User? User { get; init; }
    public string? Error { get; init; }

    public static CreateUserResult Success(User user) =>
        new() { Succeeded = true, User = user };

    public static CreateUserResult Fail(string error) =>
        new() { Succeeded = false, Error = error };
}
