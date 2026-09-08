using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portfolio_Builder.Entities.Data;
using Portfolio_Builder.Entities.DTOs;
using Portfolio_Builder.Entities.Models;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Portfolio_Builder.Services;

public class AuthenticationService(AppDbContext context, IConfiguration configuration) : IAuthenticationService
{
    public async Task<CreateUserResultDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        // Both Username and Email are unique. Check each so we can tell the
        // user exactly which one is taken. (Comparison is case-insensitive
        // because of the default SQL Server collation.)
        if (await context.Users.AnyAsync(u => u.Username == createUserDto.Username))
        {
            return CreateUserResultDto.Fail("This username is already taken.");
        }

        if (await context.Users.AnyAsync(u => u.Email == createUserDto.Email))
        {
            return CreateUserResultDto.Fail("This email is already registered.");
        }
        if(!IsValidDate(createUserDto.DateOfBirth.ToString("yyyy-MM-dd")))
        {
            return CreateUserResultDto.Fail("Invalid date of birth format. Please use YYYY-MM-DD.");
        }
        if (createUserDto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return CreateUserResultDto.Fail("Date of birth cannot be in the future.");
        }
        if (!Regex.IsMatch(createUserDto.Username, @"^(?=.{1,39}$)(?!-)(?!.*--)(?!.*-$)[A-Za-z0-9-]+$"))
        {
            return CreateUserResultDto.Fail("Invalid username format.");
        }
        if (!IsValidPassword(createUserDto.Password))
        {
            return CreateUserResultDto.Fail("Password must be at least 8 characters long and contain an uppercase, a lowercase, a digit, and a special character.");
        }
        if(await IsPasswordCompromisedAsync(createUserDto.Password))
        {
            return CreateUserResultDto.Fail("This is a very common password. Please choose a different password.");
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

        return CreateUserResultDto.Success(newUser);
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
    private static bool IsValidDate(string date)
    {
        return DateTime.TryParseExact(
            date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _
        );
    }
    private static bool IsValidPassword(string password)
    {
        return !string.IsNullOrEmpty(password)
            && password.Length >= 8
            && Regex.IsMatch(password, @"[A-Z]")
            && Regex.IsMatch(password, @"[a-z]")
            && Regex.IsMatch(password, @"\d")
            && Regex.IsMatch(password, @"[^A-Za-z0-9]");
    }
    private async Task<bool> IsPasswordCompromisedAsync(string password)
    {
        using var sha1 = SHA1.Create();

        byte[] hashBytes = sha1.ComputeHash(
            Encoding.UTF8.GetBytes(password)
        );

        string hash = Convert.ToHexString(hashBytes);

        string prefix = hash[..5];
        string suffix = hash[5..];

        using var client = new HttpClient();

        client.DefaultRequestHeaders.Add(
            "Add-Padding",
            "true"
        );

        string response = await client.GetStringAsync(
            $"https://api.pwnedpasswords.com/range/{prefix}"
        );

        foreach (string line in response.Split('\n'))
        {
            var parts = line.Trim().Split(':');

            if (parts.Length == 2 &&
                parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
