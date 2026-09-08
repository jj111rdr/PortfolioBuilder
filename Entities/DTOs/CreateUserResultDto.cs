using Portfolio_Builder.Entities.Models;

namespace Portfolio_Builder.Entities.DTOs;

/// <summary>
/// Outcome of a create-user attempt. Either it succeeded (User is set),
/// or it failed with a specific reason (Error is set).
/// </summary>
public class CreateUserResultDto
{
    public bool Succeeded { get; init; }
    public User? User { get; init; }
    public string? Error { get; init; }

    public static CreateUserResultDto Success(User user) =>
        new() { Succeeded = true, User = user };

    public static CreateUserResultDto Fail(string error) =>
        new() { Succeeded = false, Error = error };
}
