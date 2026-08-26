using Microsoft.AspNetCore.Mvc;
using Portfolio_Builder.Entities.DTOs;
using Portfolio_Builder.Services;

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
}
