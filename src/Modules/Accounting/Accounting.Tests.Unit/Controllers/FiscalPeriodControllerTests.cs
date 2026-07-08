using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for FiscalPeriodController
/// Tests all CRUD operations and status transitions
/// </summary>
public class FiscalPeriodControllerTests : AccountingUnitTestFixture
{
    private readonly Mock<IFiscalPeriodService> _serviceMock;
    private readonly Mock<ILogger<FiscalPeriodController>> _loggerMock;
    private readonly FiscalPeriodController _controller;

    public FiscalPeriodControllerTests()
    {
        _serviceMock = new Mock<IFiscalPeriodService>();
        _loggerMock = new Mock<ILogger<FiscalPeriodController>>();
        _controller = new FiscalPeriodController(_serviceMock.Object, _loggerMock.Object);
        
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

    #region CreateFiscalPeriod Tests

    [Fact]
    public async Task CreateFiscalPeriod_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateFiscalPeriodDto
        {
            FiscalCalendarId = Guid.NewGuid(),
            PeriodName = "January 2024",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31),
            Description = "Test Period"
        };

        var responseDto = new FiscalPeriodDtoBuilder()
            .WithPeriodName(request.PeriodName)
            .Build();

        _serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateFiscalPeriodDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateFiscalPeriod(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(FiscalPeriodController.GetFiscalPeriodById));
        createdResult.StatusCode.Should().Be(201);

        var response = createdResult.Value as ApiResponse<FiscalPeriodDto>;
        response?.Success.Should().BeTrue();
        response?.Message.Should().Contain("successfully");
    }

    [Fact]
    public async Task CreateFiscalPeriod_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateFiscalPeriodDto
        {
            FiscalCalendarId = Guid.NewGuid(),
            PeriodName = "",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31)
        };

        _serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateFiscalPeriodDto>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Period name is required"));

        // Act
        var result = await _controller.CreateFiscalPeriod(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);

        var errorResponse = badRequestResult.Value as ApiErrorResponse;
        errorResponse?.Message.Should().Contain("Period name is required");
    }

    #endregion

    #region GetFiscalPeriodById Tests

    [Fact]
    public async Task GetFiscalPeriodById_WhenPeriodExists_ReturnsOkWithData()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var expectedDto = new FiscalPeriodDtoBuilder().Build();

        _serviceMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetFiscalPeriodById(periodId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<FiscalPeriodDto>;
        response?.Success.Should().BeTrue();
        response?.Data.Should().Be(expectedDto);
    }

    [Fact]
    public async Task GetFiscalPeriodById_WhenPeriodNotFound_ReturnsNotFound()
    {
        // Arrange
        var periodId = Guid.NewGuid();

        _serviceMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync((FiscalPeriodDto?)null);

        // Act
        var result = await _controller.GetFiscalPeriodById(periodId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        notFoundResult.StatusCode.Should().Be(404);

        var errorResponse = notFoundResult.Value as ApiErrorResponse;
        errorResponse?.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetFiscalPeriodById_WhenServiceThrowsInvalidOperation_ReturnsUnauthorized()
    {
        // Arrange
        var periodId = Guid.NewGuid();

        _serviceMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ThrowsAsync(new InvalidOperationException("Invalid tenant context"));

        // Act
        var result = await _controller.GetFiscalPeriodById(periodId);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region GetPeriodsByCalendar Tests

    [Fact]
    public async Task GetPeriodsByCalendar_WhenCalendarHasPeriods_ReturnsOkWithCollection()
    {
        // Arrange
        var calendarId = Guid.NewGuid();
        var periods = new List<FiscalPeriodDto>
        {
            new FiscalPeriodDtoBuilder().WithFiscalCalendarId(calendarId).Build(),
            new FiscalPeriodDtoBuilder().WithFiscalCalendarId(calendarId).WithPeriodName("February").Build()
        };

        _serviceMock
            .Setup(x => x.GetByCalendarIdAsync(calendarId, It.IsAny<PaginationParams>()))
            .ReturnsAsync(PaginatedResponse<FiscalPeriodDto>.Ok(periods, periods.Count, 1, 10));

        // Act
        var result = await _controller.GetPeriodsByCalendar(calendarId, new PaginationParams());

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as PaginatedResponse<FiscalPeriodDto>;
        response?.Success.Should().BeTrue();
        response?.Data?.Count().Should().Be(2);
    }

    #endregion

    #region UpdateFiscalPeriod Tests

    [Fact]
    public async Task UpdateFiscalPeriod_WhenValidRequest_ReturnsOkWithUpdatedData()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var updateRequest = new UpdateFiscalPeriodDto
        {
            PeriodName = "Updated Period",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31)
        };

        var updatedDto = new FiscalPeriodDtoBuilder()
            .WithPeriodName("Updated Period")
            .Build();

        _serviceMock
            .Setup(x => x.UpdateAsync(periodId, It.IsAny<UpdateFiscalPeriodDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.UpdateFiscalPeriod(periodId, updateRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<FiscalPeriodDto>;
        response?.Success.Should().BeTrue();
        response?.Message.Should().Contain("successfully");
    }

    #endregion

    #region CloseFiscalPeriod Tests

    [Fact]
    public async Task CloseFiscalPeriod_WhenValidId_ReturnsNoContent()
    {
        // Arrange
        var periodId = Guid.NewGuid();

        _serviceMock
            .Setup(x => x.CloseAsync(periodId, It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CloseFiscalPeriod(periodId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var noContentResult = (NoContentResult)result;
        noContentResult.StatusCode.Should().Be(204);
    }

    #endregion

    #region DeleteFiscalPeriod Tests

    [Fact]
    public async Task DeleteFiscalPeriod_WhenValidId_ReturnsNoContent()
    {
        // Arrange
        var periodId = Guid.NewGuid();

        _serviceMock
            .Setup(x => x.DeleteAsync(periodId, It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteFiscalPeriod(periodId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var noContentResult = (NoContentResult)result;
        noContentResult.StatusCode.Should().Be(204);
    }

    #endregion
}
