using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for DimensionController
/// Tests dimension management operations
/// </summary>
public class DimensionControllerTests
{
    private readonly Mock<IDimensionService> _serviceMock;
    private readonly Mock<ILogger<DimensionController>> _loggerMock;
    private readonly DimensionController _controller;

    public DimensionControllerTests()
    {
        _serviceMock = new Mock<IDimensionService>();
        _loggerMock = new Mock<ILogger<DimensionController>>();
        _controller = new DimensionController(_serviceMock.Object, _loggerMock.Object);
        
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
    public async Task CreateDimension_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateDimensionDto
        {
            Code = "CC",
            Name = "Cost Center"
        };

        var responseDto = new DimensionDto
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name
        };

        _serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateDimensionDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateDimension(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task GetDimensionById_WhenDimensionExists_ReturnsOkWithData()
    {
        // Arrange
        var dimensionId = Guid.NewGuid();
        var expectedDto = new DimensionDto
        {
            Id = dimensionId,
            Code = "CC",
            Name = "Cost Center"
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(dimensionId))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetDimensionById(dimensionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<DimensionDto>;
        response?.Data.Should().Be(expectedDto);
    }
}
