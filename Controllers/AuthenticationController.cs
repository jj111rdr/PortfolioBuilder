using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio_Builder.Entities.DTOs;
using Portfolio_Builder.Services;
using System.Security.Claims;

namespace Portfolio_Builder.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController(IAuthenticationService authenticationService) : ControllerBase
{
    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser(CreateUserDto createUserRequest)
    {
        var result = await authenticationService.CreateUserAsync(createUserRequest);
        if (!result.Succeeded)
        {
            return Conflict(result.Error);   // 409 = the resource already exists
        }
        var message = $"User created successfully with username:{result?.User?.Username} on {result?.User?.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")}";
        return Ok(message);
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserDto loginUserRequest)
    {
        var tokenResponse = await authenticationService.AuthenticateAndLoginUserAsync(loginUserRequest);
        if (tokenResponse == null)
        {
            return Unauthorized("Invalid username or password.");
        }
        return Ok(tokenResponse);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
    {
        var token = await authenticationService.ValidateRefreshTokenAsync(request);
        if (token is null || token.AccessToken is null || token.RefreshToken is null)
        {
            return BadRequest("Invalid refresh token.");
        }
        return Ok(token);
    }
    #region Auhtication End Points
    // Protected: requires a valid JWT. Proves the whole auth loop works.
    [Authorize]
    [HttpGet("authenticated")]
    public IActionResult AuthenticatedOnlyEndpoint()
    {
        // These come from the token's claims (set in CreateToken).
        var username = User.Identity?.Name;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return Ok($"You are authenticated as {username} (role: {role}).");
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin")]
    public IActionResult AdminOnlyEndpoint()
    {
        return Ok("You are an admin.");
    }
    #endregion
}
