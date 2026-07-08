using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for FiscalCalendarController
/// Tests fiscal calendar operations
/// </summary>
public class FiscalCalendarControllerTests
{
    private readonly Mock<IFiscalCalendarService> _serviceMock;
    private readonly Mock<ILogger<FiscalCalendarController>> _loggerMock;
    private readonly FiscalCalendarController _controller;

    public FiscalCalendarControllerTests()
    {
        _serviceMock = new Mock<IFiscalCalendarService>();
        _loggerMock = new Mock<ILogger<FiscalCalendarController>>();
        _controller = new FiscalCalendarController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCalendarById_WhenCalendarExists_ReturnsOkWithData()
    {
        // Arrange
        var calendarId = Guid.NewGuid();
        var expectedDto = new FiscalCalendarDto
        {
            Id = calendarId,
            Name = "FY 2024",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(calendarId))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetCalendarById(calendarId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<FiscalCalendarDto>;
        response?.Data.Should().Be(expectedDto);
    }

    [Fact]
    public async Task GetCompanyCalendars_WhenCalendarsExist_ReturnsOkWithCollection()
    {
        // Arrange
        var calendars = new List<FiscalCalendarDto>
        {
            new FiscalCalendarDto { Id = Guid.NewGuid(), Name = "FY 2024" },
            new FiscalCalendarDto { Id = Guid.NewGuid(), Name = "FY 2025" }
        };

        _serviceMock
            .Setup(x => x.GetAllAsync(It.IsAny<PaginationParams>()))
            .ReturnsAsync(PaginatedResponse<FiscalCalendarDto>.Ok(calendars, calendars.Count, 1, 10));

        // Act
        var result = await _controller.GetCompanyCalendars(new PaginationParams());

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as PaginatedResponse<FiscalCalendarDto>;
        response?.Data?.Count().Should().Be(2);
    }
}
