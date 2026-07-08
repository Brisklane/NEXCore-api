using Core.Api.Controllers;
using Core.Application.DTOs;
using Core.Application.DTOs.ValidateDtos;
using Core.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;

namespace Core.Tests.Unit.Controllers;

public class CompanyControllerTests
{
    private readonly Mock<ICompanyService> _companyServiceMock;
    private readonly Mock<ICompanyValidator> _companyValidatorMock;
    private readonly Mock<ILogger<CompanyController>> _loggerMock;
    private readonly CompanyController _controller;

    public CompanyControllerTests()
    {
        _companyServiceMock = new Mock<ICompanyService>();
        _companyValidatorMock = new Mock<ICompanyValidator>();
        _loggerMock = new Mock<ILogger<CompanyController>>();

        _controller = new CompanyController(
            _companyServiceMock.Object,
            _loggerMock.Object,
            _companyValidatorMock.Object
        );
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    /// <summary>Sets a valid authenticated user on the controller context.</summary>
    private void SetAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>Sets an unauthenticated (empty) user on the controller context.</summary>
    private void SetUnauthenticatedUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    // =====================================================================
    // POST /api/company  —  CreateCompanyAndUser
    // =====================================================================

    [Fact]
    public async Task CreateCompanyAndUser_WhenServiceReturnsSuccess_Returns201Created()
    {
        // Arrange
        var request = new CreateCompanyAndUserDto
        {
            Company = new CompanyDto { CompanyName = "Test Company" },
            User = new CreateUserDto { Email = "admin@test.com" }
        };

        _companyServiceMock
            .Setup(x => x.CreateCompanyAndUserAsync(It.IsAny<CreateCompanyAndUserDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto>
            {
                Success = true,
                Message = "Company and User created successfully.",
                Data = request.Company
            });

        // Act
        var response = await _controller.CreateCompanyAndUser(request);

        // Assert
        var result = Assert.IsType<CreatedResult>(response);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);

        var body = Assert.IsType<ApiResponse<CompanyDto>>(result.Value);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
    }

    [Fact]
    public async Task CreateCompanyAndUser_WhenServiceReturnsFailure_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateCompanyAndUserDto
        {
            Company = new CompanyDto(),
            User = new CreateUserDto()
        };

        _companyServiceMock
            .Setup(x => x.CreateCompanyAndUserAsync(It.IsAny<CreateCompanyAndUserDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto>
            {
                Success = false,
                Message = "Company name already exists."
            });

        // Act
        var response = await _controller.CreateCompanyAndUser(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);

        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal("Company name already exists.", body.Message);
    }

    [Fact]
    public async Task CreateCompanyAndUser_WhenServiceThrows_Returns500()
    {
        // Arrange
        _companyServiceMock
            .Setup(x => x.CreateCompanyAndUserAsync(It.IsAny<CreateCompanyAndUserDto>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var response = await _controller.CreateCompanyAndUser(new CreateCompanyAndUserDto());

        // Assert
        var result = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task CreateCompanyAndUser_CallsServiceExactlyOnce()
    {
        // Arrange
        var request = new CreateCompanyAndUserDto();

        _companyServiceMock
            .Setup(x => x.CreateCompanyAndUserAsync(It.IsAny<CreateCompanyAndUserDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = new CompanyDto() });

        // Act
        await _controller.CreateCompanyAndUser(request);

        // Assert
        _companyServiceMock.Verify(x => x.CreateCompanyAndUserAsync(request), Times.Once);
    }

    // =====================================================================
    // GET /api/company/{id}  —  GetCompany
    // =====================================================================

    [Fact]
    public async Task GetCompany_WhenCompanyExists_Returns200WithData()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        _companyServiceMock
            .Setup(x => x.GetCompanyByIdAsync(companyId))
            .ReturnsAsync(Result<CompanyDto>.Ok(new CompanyDto
            {
                CompanyName = "Test Company",
                Email = "test@company.com"
            }));

        // Act
        var response = await _controller.GetCompany(companyId);

        // Assert
        var result = Assert.IsType<OkObjectResult>(response);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

        var body = Assert.IsType<ApiResponse<CompanyDto>>(result.Value);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
    }

    [Fact]
    public async Task GetCompany_WhenCompanyDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        _companyServiceMock
            .Setup(x => x.GetCompanyByIdAsync(companyId))
            .ReturnsAsync(Result<CompanyDto>.Fail("Company not found"));

        // Act
        var response = await _controller.GetCompany(companyId);

        // Assert
        var result = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);

        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal("Company not found", body.Message);
    }

    [Fact]
    public async Task GetCompany_WhenServiceThrows_Returns500()
    {
        // Arrange
        _companyServiceMock
            .Setup(x => x.GetCompanyByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var response = await _controller.GetCompany(Guid.NewGuid());

        // Assert
        var result = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task GetCompany_CallsServiceWithCorrectId()
    {
        // Arrange
        var companyId = Guid.NewGuid();

        _companyServiceMock
            .Setup(x => x.GetCompanyByIdAsync(companyId))
            .ReturnsAsync(Result<CompanyDto>.Ok(new CompanyDto()));

        // Act
        await _controller.GetCompany(companyId);

        // Assert
        _companyServiceMock.Verify(x => x.GetCompanyByIdAsync(companyId), Times.Once);
    }

    // =====================================================================
    // PUT /api/company/{id}  —  UpdateCompany
    // =====================================================================

    [Fact]
    public async Task UpdateCompany_WhenValidUserAndServiceSucceeds_Returns200()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetAuthenticatedUser(userId);

        var request = new UpdateCompanyRequest
        {
            CompanyName = "Updated Company",
            LegalName = "Updated Legal Name",
            BaseCurrencyCode = "USD",
            PhoneNumber = "+1234567890",
            MobileNumber = "+1234567891",
            ContactPerson = "Jane Doe",
            WebsiteUrl = "https://updated.com",
            CompanyLogo = new byte[] { 1, 2, 3 },
            Latitude = 40.7128m,
            Longitude = -74.0060m,
            RadiusInMeters = 500,
            StreetAddress = "123 Main St",
            City = "New York",
            State = "NY",
            PostalCode = "10001"
        };

        _companyServiceMock
            .Setup(x => x.UpdateCompanyAsync(companyId, request, userId))
            .ReturnsAsync(Result<CompanyDto>.Ok(new CompanyDto
            {
                CompanyName = request.CompanyName
            }, "Company updated successfully."));

        // Act
        var response = await _controller.UpdateCompany(companyId, request);

        // Assert
        var result = Assert.IsType<OkObjectResult>(response);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

        var body = Assert.IsType<ApiResponse<CompanyDto>>(result.Value);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
    }

    [Fact]
    public async Task UpdateCompany_WhenNoUserClaimPresent_Returns401Unauthorized()
    {
        // Arrange
        SetUnauthenticatedUser();

        // Act
        var response = await _controller.UpdateCompany(Guid.NewGuid(), new UpdateCompanyRequest());

        // Assert
        var result = Assert.IsType<UnauthorizedObjectResult>(response);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);

        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal("Invalid user", body.Message);
    }

    [Fact]
    public async Task UpdateCompany_WhenUserClaimIsNotAGuid_Returns401Unauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };

        // Act
        var response = await _controller.UpdateCompany(Guid.NewGuid(), new UpdateCompanyRequest());

        // Assert
        var result = Assert.IsType<UnauthorizedObjectResult>(response);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task UpdateCompany_WhenServiceReturnsFailure_Returns400BadRequest()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetAuthenticatedUser(userId);

        _companyServiceMock
            .Setup(x => x.UpdateCompanyAsync(companyId, It.IsAny<UpdateCompanyRequest>(), userId))
            .ReturnsAsync(Result<CompanyDto>.Fail("Company not found"));

        // Act
        var response = await _controller.UpdateCompany(companyId, new UpdateCompanyRequest());

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);

        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal("Company not found", body.Message);
    }

    [Fact]
    public async Task UpdateCompany_WhenServiceThrows_Returns500()
    {
        // Arrange
        SetAuthenticatedUser(Guid.NewGuid());

        _companyServiceMock
            .Setup(x => x.UpdateCompanyAsync(It.IsAny<Guid>(), It.IsAny<UpdateCompanyRequest>(), It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var response = await _controller.UpdateCompany(Guid.NewGuid(), new UpdateCompanyRequest());

        // Assert
        var result = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task UpdateCompany_CallsServiceWithCorrectCompanyIdAndUserId()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetAuthenticatedUser(userId);

        var request = new UpdateCompanyRequest();

        _companyServiceMock
            .Setup(x => x.UpdateCompanyAsync(companyId, request, userId))
            .ReturnsAsync(Result<CompanyDto>.Ok(new CompanyDto()));

        // Act
        await _controller.UpdateCompany(companyId, request);

        // Assert
        _companyServiceMock.Verify(x => x.UpdateCompanyAsync(companyId, request, userId), Times.Once);
    }

    // =====================================================================
    // POST /api/company/validate  —  ValidateCompany
    // =====================================================================

    [Fact]
    public async Task ValidateCompany_WhenValidationPasses_Returns200WithData()
    {
        // Arrange
        var request = new CompanyDto { CompanyName = "Valid Company" };

        _companyValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto>
            {
                Success = true,
                Message = "Validation passed. You may proceed.",
                Data = request
            });

        // Act
        var response = await _controller.ValidateCompany(request);

        // Assert
        var result = Assert.IsType<OkObjectResult>(response);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

        var body = Assert.IsType<ApiResponse<CompanyDto>>(result.Value);
        Assert.True(body.Success);
        Assert.Equal("Validation passed. You may proceed.", body.Message);
        Assert.NotNull(body.Data);
    }

    [Fact]
    public async Task ValidateCompany_WhenValidationFails_Returns400BadRequest()
    {
        // Arrange
        var request = new CompanyDto { CompanyName = "" };

        _companyValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto>
            {
                Success = false,
                Message = "Company name is required."
            });

        // Act
        var response = await _controller.ValidateCompany(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);

        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal("Company name is required.", body.Message);
    }

    [Fact]
    public async Task ValidateCompany_WhenValidatorThrows_Returns500()
    {
        // Arrange
        _companyValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ThrowsAsync(new Exception("Validator crashed"));

        // Act
        var response = await _controller.ValidateCompany(new CompanyDto());

        // Assert
        var result = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task ValidateCompany_CallsValidatorExactlyOnce()
    {
        // Arrange
        var request = new CompanyDto();

        _companyValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = request });

        // Act
        await _controller.ValidateCompany(request);

        // Assert
        _companyValidatorMock.Verify(x => x.ValidateAsync(request), Times.Once);
    }
}
