using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for PostingProfileController
/// Tests posting profile operations
/// </summary>
public class PostingProfileControllerTests
{
    private readonly Mock<IPostingProfileService> _serviceMock;
    private readonly Mock<ILogger<PostingProfileController>> _loggerMock;
    private readonly PostingProfileController _controller;

    public PostingProfileControllerTests()
    {
        _serviceMock = new Mock<IPostingProfileService>();
        _loggerMock = new Mock<ILogger<PostingProfileController>>();
        _controller = new PostingProfileController(_serviceMock.Object, _loggerMock.Object);
        
        // Setup user with claims
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("company_id", Guid.NewGuid().ToString()),
            new Claim("branch_id", Guid.NewGuid().ToString()),
            new Claim("business_unit_id", Guid.NewGuid().ToString())
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task CreatePostingProfile_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreatePostingProfileDto
        {
            ModuleName = "Inventory",
            TransactionType = "PO",
            DebitAccountId = Guid.NewGuid(),
            CreditAccountId = Guid.NewGuid()
        };

        var responseDto = new PostingProfileDto
        {
            Id = Guid.NewGuid(),
            ModuleName = request.ModuleName,
            TransactionType = request.TransactionType
        };

        _serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreatePostingProfileDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreatePostingProfile(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task GetAllProfiles_WhenProfilesExist_ReturnsOkWithCollection()
    {
        // Arrange
        var profiles = new List<PostingProfileDto>
        {
            new PostingProfileDto { Id = Guid.NewGuid(), ModuleName = "Inventory", TransactionType = "PO" },
            new PostingProfileDto { Id = Guid.NewGuid(), ModuleName = "Sales", TransactionType = "Invoice" }
        };

        _serviceMock
            .Setup(x => x.GetAllAsync(It.IsAny<PaginationParams>()))
            .ReturnsAsync(PaginatedResponse<PostingProfileDto>.Ok(profiles, profiles.Count, 1, 10));

        // Act
        var result = await _controller.GetAllProfiles(new PaginationParams());

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as PaginatedResponse<PostingProfileDto>;
        response?.Data?.Count().Should().Be(2);
    }
}
