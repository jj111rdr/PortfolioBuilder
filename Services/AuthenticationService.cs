using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portfolio_Builder.Entities.Data;
using Portfolio_Builder.Entities.DTOs;
using Portfolio_Builder.Entities.Models;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace Portfolio_Builder.Services;

public class AuthenticationService(AppDbContext context, IConfiguration configuration) : IAuthenticationService
{
    public async Task<CreateUserResult> CreateUserAsync(CreateUserDto createUserDto)
    {
        // Both Username and Email are unique. Check each so we can tell the
        // user exactly which one is taken. (Comparison is case-insensitive
        // because of the default SQL Server collation.)
        if (await context.Users.AnyAsync(u => u.Username == createUserDto.Username))
        {
            return CreateUserResult.Fail("This username is already taken.");
        }

        if (await context.Users.AnyAsync(u => u.Email == createUserDto.Email))
        {
            return CreateUserResult.Fail("This email is already registered.");
        }

        var newUser = new User
        {
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            DateOfBirth = createUserDto.DateOfBirth,
            Gender = createUserDto.Gender,
            Email = createUserDto.Email,
            Username = createUserDto.Username,
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };

        newUser.PasswordHash = new PasswordHasher<User>().HashPassword(newUser, createUserDto.Password);

        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        return CreateUserResult.Success(newUser);
    }
    public async Task<TokenResponseDto?> AuthenticateAndLoginUserAsync(LoginUserDto loginUserDto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == loginUserDto.Username);
        if (user is null)
        {
            return null;
        }
        if(new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, loginUserDto.Password) == PasswordVerificationResult.Failed)
        {
            return null;
        }
        var token = CreateToken(user);
        var refreshToken = await GenerateAndSaveRefreshToken(user);
        TokenResponseDto tokenResponse = await Task.FromResult(new TokenResponseDto 
        { 
            AccessToken = token, 
            RefreshToken = refreshToken 
        });
        return tokenResponse;
    }
    string CreateToken(User user)
    {
        var userClaims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                configuration.GetValue<string>("AppSettings:Token")!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: configuration.GetValue<string>("AppSettings:Issuer"),
            audience: configuration.GetValue<string>("AppSettings:Audience"),
            claims: userClaims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwtToken);
    }
    async Task<string> GenerateAndSaveRefreshToken(User user)
    {
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpireTime = DateTime.UtcNow.AddDays(1);
        context.Users.Update(user);
        await context.SaveChangesAsync();
        return refreshToken;
    }
    string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using ( var randomNumberGenerated = RandomNumberGenerator.Create())
        {
            randomNumberGenerated.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    public async Task<TokenResponseDto?> ValidateRefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequestDto)
    {
        var user = await context.Users.FindAsync(refreshTokenRequestDto.UserId);
        if (user is null || user.RefreshToken != refreshTokenRequestDto.RefreshToken || user.RefreshTokenExpireTime < DateTime.UtcNow)
        {
            return null;
        }
        var token = CreateToken(user);
        var newRefreshToken = await GenerateAndSaveRefreshToken(user);
        return new TokenResponseDto
        {
            AccessToken = token,
            RefreshToken = newRefreshToken
        };
    }
}
