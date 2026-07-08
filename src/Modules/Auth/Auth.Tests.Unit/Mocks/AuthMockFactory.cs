using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Auth.Tests.Unit.Mocks;

/// <summary>
/// Common mocks for Auth module unit tests
/// </summary>
public static class AuthMockFactory
{
    public static Mock<IPasswordHasher<User>> CreatePasswordHasherMock()
    {
        var mock = new Mock<IPasswordHasher<User>>();
        
        mock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns((User u, string password) => $"hashed_{password}");
        
        mock.Setup(x => x.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((User u, string hash, string password) => 
                hash == $"hashed_{password}" ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed);

        return mock;
    }

    public static User CreateValidUser()
    {
        return new UserBuilder()
            .WithUsername("validuser")
            .WithEmail("valid@example.com")
            .WithFullName("Valid User")
            .WithIsActive(true)
            .WithIsEmailVerified(true)
            .Build();
    }

    public static User CreateInactiveUser()
    {
        return new UserBuilder()
            .WithUsername("inactiveuser")
            .WithEmail("inactive@example.com")
            .WithFullName("Inactive User")
            .WithIsActive(false)
            .Build();
    }

    public static User CreateLockedOutUser()
    {
        return new UserBuilder()
            .WithUsername("lockeduser")
            .WithEmail("locked@example.com")
            .WithFullName("Locked User")
            .WithIsLockedOut(true)
            .WithFailedLoginAttempts(5)
            .Build();
    }
}
