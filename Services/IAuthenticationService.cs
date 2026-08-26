using Portfolio_Builder.Entities.DTOs;

namespace Portfolio_Builder.Services;

public interface IAuthenticationService
{
    Task<CreateUserResult> CreateUserAsync(CreateUserDto createUserDto);
}
