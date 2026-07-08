using Auth.Api.Controllers;
using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;

namespace Auth.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for AuthController.
/// Covers only the four active endpoints: Register, ValidateUser, Login, Logout.
/// IUserService is injected by the controller constructor but unused in these four
/// endpoints, so it is mocked with no setup required.
/// </summary>
public class AuthControllerTests
{
    // ------------------------------------------------------------------ deps
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<IUserService> _userServiceMock;   // injected but unused here
    private readonly Mock<IUserValidator> _userValidatorMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthenticationService>();
        _userServiceMock = new Mock<IUserService>();
        _userValidatorMock = new Mock<IUserValidator>();
        _loggerMock = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _authServiceMock.Object,
            _loggerMock.Object,
            _userServiceMock.Object,
            _userValidatorMock.Object
        );
    }

    // ------------------------------------------------------------------ helpers
    /// <summary>
    /// Sets up the controller's HttpContext with a ClaimsPrincipal that carries
    /// the supplied userId as NameIdentifier � required by the [Authorize] Logout endpoint.
    /// </summary>
    private void SetUserClaims(Guid userId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>Sets an empty ClaimsPrincipal (no NameIdentifier claim).</summary>
    private void SetEmptyClaims()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private static CreateUserDto ValidCreateUserDto() => new()
    {
        UserName = "newuser",
        Email = "newuser@example.com",
        FullName = "New User",
        Password = "Password123!",
        PhoneNumber = "+1234567890",
        Gender = "Male",
        DateOfBirth = "1990-01-01",
        Address = "123 Test Street",
        TenantId = Guid.NewGuid(),
        CompanyId = Guid.NewGuid(),
        CompanyName = "Test Company",
        CompanySlug = "test-company",
        BranchId = Guid.NewGuid(),
        BusinessUnitId = Guid.NewGuid()
    };

    private static ValidateUserDto ValidValidateUserDto() => new()
    {
        UserName = "validuser",
        Email = "valid@example.com",
        FullName = "Valid User",
        Password = "Password123!",
        PhoneNumber = "+1234567890",
        Gender = "Male",
        DateOfBirth = "1990-01-01",
        Address = "123 Test Street"
    };

    private static LoginRequest ValidLoginRequest() => new()
    {
        Username = "testuser",
        Password = "Password123!"
    };

    private static LoginResponse FakeLoginResponse() => new()
    {
        AccessToken = "fake_access_token",
        RefreshToken = "fake_refresh_token",
        ExpiresIn = 900,
        User = new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            CompanyId = Guid.NewGuid(),
            IsActive = true,
            IsEmailVerified = true,
            Roles = [],
            Permissions = []
        }
    };

    // ==================================================================
    // REGISTER
    // ==================================================================
    #region Register

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk_WithSuccessTrue()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(new ApiResponse { Success = true, Message = "User registered successfully" });

        // Act
        var result = await _controller.Register(ValidCreateUserDto());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);

        var body = Assert.IsType<ApiResponse>(ok.Value);
        body.Success.Should().BeTrue();
        body.Message.Should().Be("User registered successfully");
    }

    [Fact]
    public async Task Register_ServiceReturnsFailure_ReturnsBadRequest()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(new ApiResponse { Success = false, Message = "Email is already registered." });

        // Act
        var result = await _controller.Register(ValidCreateUserDto());

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        bad.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var body = Assert.IsType<ApiErrorResponse>(bad.Value);
        body.Message.Should().Be("Email is already registered.");
    }

    [Fact]
    public async Task Register_ServiceThrows_Returns500()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<CreateUserDto>()))
            .ThrowsAsync(new Exception("DB connection lost"));

        // Act
        var result = await _controller.Register(ValidCreateUserDto());

        // Assert
        var obj = Assert.IsType<ObjectResult>(result);
        obj.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Register_CallsAuthService_ExactlyOnce()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(new ApiResponse { Success = true });

        // Act
        await _controller.Register(ValidCreateUserDto());

        // Assert
        _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<CreateUserDto>()), Times.Once);
    }

    #endregion

    // ==================================================================
    // VALIDATE USER
    // ==================================================================
    #region ValidateUser

    [Fact]
    public async Task ValidateUser_ValidData_ReturnsOk_WithSuccessMessage()
    {
        // Arrange
        _userValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<ValidateUserDto>()))
            .ReturnsAsync(new ApiResponse { Success = true });

        // Act
        var result = await _controller.ValidateUser(ValidValidateUserDto());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);

        var body = Assert.IsType<ApiResponse>(ok.Value);
        body.Success.Should().BeTrue();
        // The controller hard-codes this message regardless of what the service returns
        body.Message.Should().Be("Validation passed. You may proceed.");
    }

    [Fact]
    public async Task ValidateUser_ValidationFails_ReturnsBadRequest()
    {
        // Arrange
        _userValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<ValidateUserDto>()))
            .ReturnsAsync(new ApiResponse { Success = false, Message = "Invalid email format." });

        // Act
        var result = await _controller.ValidateUser(ValidValidateUserDto());

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        bad.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var body = Assert.IsType<ApiErrorResponse>(bad.Value);
        body.Message.Should().Be("Invalid email format.");
    }

    [Fact]
    public async Task ValidateUser_ServiceThrows_Returns500()
    {
        // Arrange
        _userValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<ValidateUserDto>()))
            .ThrowsAsync(new Exception("unexpected"));

        // Act
        var result = await _controller.ValidateUser(ValidValidateUserDto());

        // Assert
        var obj = Assert.IsType<ObjectResult>(result);
        obj.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ValidateUser_CallsValidator_ExactlyOnce()
    {
        // Arrange
        _userValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<ValidateUserDto>()))
            .ReturnsAsync(new ApiResponse { Success = true });

        // Act
        await _controller.ValidateUser(ValidValidateUserDto());

        // Assert � auth service must NOT be touched during validate
        _userValidatorMock.Verify(x => x.ValidateAsync(It.IsAny<ValidateUserDto>()), Times.Once);
        _authServiceMock.VerifyNoOtherCalls();
    }

    #endregion

    // ==================================================================
    // LOGIN
    // ==================================================================
    #region Login

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk_WithTokens()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Ok(FakeLoginResponse(), "Login successful"));

        // Act
        var result = await _controller.Login(ValidLoginRequest());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);

        var body = Assert.IsType<ApiResponse<LoginResponse>>(ok.Value);
        body.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Fail("Invalid username or password"));

        // Act
        var result = await _controller.Login(ValidLoginRequest());

        // Assert
        var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
        unauth.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var body = Assert.IsType<ApiErrorResponse>(unauth.Value);
        body.Message.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task Login_LockedOutAccount_ReturnsUnauthorized_WithLockMessage()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Fail("Account is locked. Please try again later."));

        // Act
        var result = await _controller.Login(ValidLoginRequest());

        // Assert
        var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
        var body = Assert.IsType<ApiErrorResponse>(unauth.Value);
        body.Message.Should().Contain("locked");
    }

    [Fact]
    public async Task Login_ServiceThrows_Returns500()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new Exception("DB timeout"));

        // Act
        var result = await _controller.Login(ValidLoginRequest());

        // Assert
        var obj = Assert.IsType<ObjectResult>(result);
        obj.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Login_CallsAuthService_ExactlyOnce()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(Result<LoginResponse>.Ok(FakeLoginResponse()));

        // Act
        await _controller.Login(ValidLoginRequest());

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>()), Times.Once);
    }

    #endregion

    // ==================================================================
    // LOGOUT
    // ==================================================================
    #region Logout

    [Fact]
    public async Task Logout_ValidUserAndToken_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUserClaims(userId);

        _authServiceMock
            .Setup(x => x.LogoutAsync(userId, It.IsAny<string>()))
            .ReturnsAsync(Result.Ok("Logout successful"));

        // Act
        var result = await _controller.Logout(new LogoutRequest { RefreshToken = "valid_token" });

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);

        var body = Assert.IsType<ApiResponse>(ok.Value);
        body.Success.Should().BeTrue();
        body.Message.Should().Be("Logout successful");
    }

    [Fact]
    public async Task Logout_MissingNameIdentifierClaim_ReturnsUnauthorized()
    {
        // Arrange � no NameIdentifier in claims means Guid.TryParse fails
        SetEmptyClaims();

        // Act
        var result = await _controller.Logout(new LogoutRequest { RefreshToken = "some_token" });

        // Assert
        var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
        unauth.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        // LogoutAsync must never be called when user identity cannot be resolved
        _authServiceMock.Verify(x => x.LogoutAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Logout_NonGuidNameIdentifier_ReturnsUnauthorized()
    {
        // Arrange � claim exists but value is not a valid Guid
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "not-a-guid") };
        var identity = new ClaimsIdentity(claims);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _controller.Logout(new LogoutRequest { RefreshToken = "token" });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
        _authServiceMock.Verify(x => x.LogoutAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Logout_PassesCorrectUserIdToService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUserClaims(userId);

        _authServiceMock
            .Setup(x => x.LogoutAsync(userId, "my_token"))
            .ReturnsAsync(Result.Ok("Logout successful"));

        // Act
        await _controller.Logout(new LogoutRequest { RefreshToken = "my_token" });

        // Assert � the exact userId parsed from claims is forwarded
        _authServiceMock.Verify(x => x.LogoutAsync(userId, "my_token"), Times.Once);
    }

    [Fact]
    public async Task Logout_ServiceThrows_Returns500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUserClaims(userId);

        _authServiceMock
            .Setup(x => x.LogoutAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _controller.Logout(new LogoutRequest { RefreshToken = "token" });

        // Assert
        var obj = Assert.IsType<ObjectResult>(result);
        obj.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    #endregion
}
