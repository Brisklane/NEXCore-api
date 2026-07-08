using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Interfaces;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Auth.Tests.Unit.Services;

/// <summary>
/// Unit tests for AuthenticationService.
/// Covers RegisterAsync, LoginAsync, and LogoutAsync only.
///
/// Strategy:
///  - AuthDbContext  → in-memory EF Core (fresh DB per test via unique DB name)
///  - IPasswordHasher → AuthMockFactory.CreatePasswordHasherMock()
///  - ITokenService   → Moq
///  - IRoleService    → Moq
///  - IUserValidator  → Moq (RegisterAsync delegates to it first)
///  - IPasswordService is NOT used by AuthenticationService; ignored entirely.
/// </summary>
public class AuthenticationServiceTests : AuthUnitTestFixture
{
    // ------------------------------------------------------------------ factory
    /// <summary>
    /// Creates a fresh in-memory AuthDbContext and a fully-wired
    /// AuthenticationService. All mocks are returned via out-params so
    /// individual tests can configure extra setups.
    /// </summary>
    private static (
        AuthenticationService service,
        AuthDbContext db,
        Mock<IPasswordHasher<User>> hasherMock,
        Mock<ITokenService> tokenMock,
        Mock<IRoleService> roleMock,
        Mock<IUserValidator> validatorMock)
        CreateSut(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var db = new AuthDbContext(options);
        var hasherMock = AuthMockFactory.CreatePasswordHasherMock();
        var tokenMock = new Mock<ITokenService>();
        var roleMock = new Mock<IRoleService>();
        var validatorMock = new Mock<IUserValidator>();

        // Default token stubs — most login tests need these
        tokenMock.Setup(t => t.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                 .Returns("fake_access_token");
        tokenMock.Setup(t => t.GeneratePreAuthToken(It.IsAny<User>()))
                 .Returns("fake_pre_auth_token");
        tokenMock.Setup(t => t.GenerateRefreshToken())
                 .Returns("fake_refresh_token");

        // Default role/permission stubs
        roleMock.Setup(r => r.GetUserRolesAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<string> { "Admin" });
        roleMock.Setup(r => r.GetUserPermissionsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<string> { "USER_VIEW" });

        // Default validator stub — succeeds unless overridden
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidateUserDto>()))
                     .ReturnsAsync(new Nexcore.SharedKernel.Api.ApiResponse { Success = true, Message = "Validation passed." });

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:AccessTokenExpirationMinutes"]).Returns("15");

        var service = new AuthenticationService(
            db,
            new Mock<Auth.Domain.Interfaces.IPasswordService>().Object,
            tokenMock.Object,
            roleMock.Object,
            hasherMock.Object,
            validatorMock.Object,
            new Mock<Nexcore.SharedKernel.Events.IEventPublisher>().Object,
            configMock.Object
        );

        return (service, db, hasherMock, tokenMock, roleMock, validatorMock);
    }

    // helper — seeds a user whose password hash matches the mock hasher convention
    private static User SeedUser(AuthDbContext db, string password = "Password123!", Action<User>? configure = null)
    {
        var hasher = new PasswordHasher<User>();
        var user = new UserBuilder()
            .WithUsername("testuser")
            .WithEmail("test@example.com")
            .WithFullName("Test User")
            .WithIsActive(true)
            .Build();

        // Use the same mock convention: "hashed_{password}"
        user.PasswordHash = $"hashed_{password}";

        configure?.Invoke(user);

        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    // ==================================================================
    // REGISTER
    // ==================================================================
    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccess_AndPersistsUser()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();

        var dto = new CreateUserDto
        {
            UserName = "newuser",
            Email = "new@example.com",
            FullName = "New User",
            Password = "Password123!",
            PhoneNumber = "+1234567890",
            Gender = "Male",
            DateOfBirth = "1990-01-01",
            Address = "123 Test Street",
            CompanyId = Guid.NewGuid()
        };

        // Act
        var result = await service.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("User registered successfully");

        var saved = await db.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
        saved.Should().NotBeNull();
        saved!.Email.Should().Be("new@example.com");
        saved.IsActive.Should().BeTrue();
        saved.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsHashed_NotStoredPlainText()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        const string plainPassword = "Password123!";

        var dto = new CreateUserDto
        {
            UserName = "hashuser",
            Email = "hash@example.com",
            FullName = "Hash User",
            Password = plainPassword,
            PhoneNumber = "+1234567890",
            Gender = "Male",
            DateOfBirth = "1990-01-01",
            Address = "123 Street",
            CompanyId = Guid.NewGuid()
        };

        // Act
        await service.RegisterAsync(dto);

        // Assert — the mock hasher prepends "hashed_", plain text must not be stored
        var saved = await db.Users.FirstOrDefaultAsync(u => u.Username == "hashuser");
        saved.Should().NotBeNull();
        saved!.PasswordHash.Should().NotBe(plainPassword);
        saved.PasswordHash.Should().Be($"hashed_{plainPassword}");
    }

    [Fact]
    public async Task RegisterAsync_ValidatorFails_ReturnsFailure_UserNotPersisted()
    {
        // Arrange
        var (service, db, _, _, _, validatorMock) = CreateSut();

        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidateUserDto>()))
                     .ReturnsAsync(new Nexcore.SharedKernel.Api.ApiResponse
                     {
                         Success = false,
                         Message = "Email is already registered."
                     });

        var dto = new CreateUserDto
        {
            UserName = "dupuser",
            Email = "dup@example.com",
            FullName = "Dup User",
            Password = "Password123!",
            PhoneNumber = "+1234567890",
            Gender = "Male",
            DateOfBirth = "1990-01-01",
            Address = "123 Street",
            CompanyId = Guid.NewGuid()
        };

        // Act
        var result = await service.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email is already registered.");

        var count = await db.Users.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task RegisterAsync_ValidatorCalled_WithMappedFields()
    {
        // Arrange
        var (service, _, _, _, _, validatorMock) = CreateSut();

        ValidateUserDto? captured = null;
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidateUserDto>()))
                     .Callback<ValidateUserDto>(dto => captured = dto)
                     .ReturnsAsync(new Nexcore.SharedKernel.Api.ApiResponse { Success = true });

        var request = new CreateUserDto
        {
            UserName = "mapuser",
            Email = "map@example.com",
            FullName = "Map User",
            Password = "Password123!",
            PhoneNumber = "+0987654321",
            Gender = "Female",
            DateOfBirth = "1995-06-15",
            Address = "456 Another St",
            CompanyId = Guid.NewGuid()
        };

        // Act
        await service.RegisterAsync(request);

        // Assert — every field that RegisterAsync maps must reach the validator
        captured.Should().NotBeNull();
        captured!.Email.Should().Be(request.Email);
        captured.UserName.Should().Be(request.UserName);
        captured.Password.Should().Be(request.Password);
        captured.FullName.Should().Be(request.FullName);
        captured.Gender.Should().Be(request.Gender);
        captured.DateOfBirth.Should().Be(request.DateOfBirth);
        captured.PhoneNumber.Should().Be(request.PhoneNumber);
        captured.Address.Should().Be(request.Address);
    }

    #endregion

    // ==================================================================
    // LOGIN
    // ==================================================================
    #region LoginAsync

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess_WithTokens()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        SeedUser(db, "Password123!");

        // Act
        var result = await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Login successful");
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("fake_access_token");
        result.Data.RefreshToken.Should().Be("fake_refresh_token");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_SavesRefreshToken()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        var user = SeedUser(db, "Password123!");

        // Act
        await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });

        // Assert — login issues a refresh token immediately
        var count = await db.RefreshTokens.CountAsync(t => t.UserId == user.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var (service, _, _, _, _, _) = CreateSut();
        // No users seeded

        // Act
        var result = await service.LoginAsync(new LoginRequest { Username = "ghost", Password = "Password123!" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsFailure()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        SeedUser(db, "Password123!", u => u.IsActive = false);

        // Act
        var result = await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });

        // Assert — inactive users are filtered at query level (IsActive check in Where clause)
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFailure()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        SeedUser(db, "Password123!");

        // Act
        var result = await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "WrongPassword!" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_IncrementsFailedLoginAttempts()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        var user = SeedUser(db, "Password123!");

        // Act
        await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "WrongPassword!" });

        // Assert
        var updated = await db.Users.FindAsync(user.Id);
        updated!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_FiveFailedAttempts_LocksAccount()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        SeedUser(db, "Password123!", u => u.FailedLoginAttempts = 4);

        // Act — 5th failed attempt
        await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "WrongPassword!" });

        // Assert
        var updated = await db.Users.FirstAsync(u => u.Username == "testuser");
        updated.IsLockedOut.Should().BeTrue();
        updated.LockoutEndAt.Should().NotBeNull();
        updated.LockoutEndAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_LockedOutAccount_ReturnsLockedMessage()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        SeedUser(db, "Password123!", u =>
        {
            u.IsLockedOut = true;
            u.LockoutEndAt = DateTime.UtcNow.AddMinutes(25); // still locked
        });

        // Act
        var result = await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("locked");
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_ResetsFailedAttempts()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        SeedUser(db, "Password123!", u => u.FailedLoginAttempts = 3);

        // Act
        await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });

        // Assert
        var updated = await db.Users.FirstAsync(u => u.Username == "testuser");
        updated.FailedLoginAttempts.Should().Be(0);
        updated.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_SetsLastLoginAt()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        var before = DateTime.UtcNow;
        SeedUser(db, "Password123!");

        // Act
        await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });

        // Assert
        var updated = await db.Users.FirstAsync(u => u.Username == "testuser");
        updated.LastLoginAt.Should().NotBeNull();
        updated.LastLoginAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_SucceedsWithoutUserCompanyRecord()
    {
        // Arrange — user exists but has no UserCompany records (login does not require them)
        var (service, db, _, _, _, _) = CreateSut();
        SeedUser(db, "Password123!");

        // Act
        var result = await service.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
    }

    #endregion

    // ==================================================================
    // LOGOUT
    // ==================================================================
    #region LogoutAsync

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesToken_ReturnsSuccess()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        var user = SeedUser(db);

        var token = new RefreshToken
        {
            UserId = user.Id,
            Token = "valid_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            CompanyId = user.CompanyId
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        // Act
        var result = await service.LogoutAsync(user.Id, "valid_token");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Logout successful");

        var updated = await db.RefreshTokens.FirstAsync(t => t.Token == "valid_token");
        updated.IsRevoked.Should().BeTrue();
        updated.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LogoutAsync_TokenNotFound_StillReturnsSuccess()
    {
        // Arrange — no tokens seeded; service handles missing token gracefully
        var (service, db, _, _, _, _) = CreateSut();
        var user = SeedUser(db);

        // Act
        var result = await service.LogoutAsync(user.Id, "nonexistent_token");

        // Assert — logout is idempotent; not finding the token is not an error
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_TokenBelongsToDifferentUser_NotRevoked()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        var user = SeedUser(db);
        var otherUser = new UserBuilder().WithUsername("other").WithEmail("other@example.com").Build();
        db.Users.Add(otherUser);

        var token = new RefreshToken
        {
            UserId = otherUser.Id,   // belongs to OTHER user
            Token = "other_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = otherUser.Id,
            CompanyId = otherUser.CompanyId
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        // Act — logout with user.Id but the token belongs to otherUser.Id
        await service.LogoutAsync(user.Id, "other_token");

        // Assert — token must NOT be revoked because the UserId doesn't match
        var unchanged = await db.RefreshTokens.FirstAsync(t => t.Token == "other_token");
        unchanged.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_RevokedAtTimestamp_IsSetToUtcNow()
    {
        // Arrange
        var (service, db, _, _, _, _) = CreateSut();
        var user = SeedUser(db);
        var before = DateTime.UtcNow;

        var token = new RefreshToken
        {
            UserId = user.Id,
            Token = "timed_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            CompanyId = user.CompanyId
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        // Act
        await service.LogoutAsync(user.Id, "timed_token");

        // Assert
        var updated = await db.RefreshTokens.FirstAsync(t => t.Token == "timed_token");
        updated.RevokedAt.Should().NotBeNull();
        updated.RevokedAt.Should().BeOnOrAfter(before);
    }

    #endregion
}
