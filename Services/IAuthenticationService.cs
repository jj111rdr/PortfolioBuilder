using Portfolio_Builder.Entities.DTOs;

namespace Portfolio_Builder.Services;

public interface IAuthenticationService
{
    Task<CreateUserResult> CreateUserAsync(CreateUserDto createUserDto);
    Task<TokenResponseDto?> AuthenticateAndLoginUserAsync(LoginUserDto loginUserDto);
    Task<TokenResponseDto?> ValidateRefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequestDto);
}
    