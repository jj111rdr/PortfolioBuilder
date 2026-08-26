using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portfolio_Builder.Entities.Data;
using Portfolio_Builder.Entities.DTOs;
using Portfolio_Builder.Entities.Models;

namespace Portfolio_Builder.Services;

public class AuthenticationService(AppDbContext context) : IAuthenticationService
{
    private static readonly PasswordHasher<User> PasswordHasher = new();

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

        newUser.PasswordHash = PasswordHasher.HashPassword(newUser, createUserDto.Password);

        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        return CreateUserResult.Success(newUser);
    }
}
