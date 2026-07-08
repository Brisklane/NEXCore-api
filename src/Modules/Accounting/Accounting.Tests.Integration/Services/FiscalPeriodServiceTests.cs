using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Moq;
using System.Linq.Expressions;

namespace Accounting.Tests.Integration.Services;

/// <summary>
/// Integration-style tests for FiscalPeriodService
/// Tests service logic with mocked repository to verify business rules
/// </summary>
public class FiscalPeriodServiceTests
{
    private readonly Mock<IFiscalPeriodRepository> _repositoryMock;
    private readonly Mock<ILogger<FiscalPeriodService>> _loggerMock;
    private readonly IFiscalPeriodService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public FiscalPeriodServiceTests()
    {
        _repositoryMock = new Mock<IFiscalPeriodRepository>();
        _loggerMock = new Mock<ILogger<FiscalPeriodService>>();
        _service = new FiscalPeriodService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CallsRepositoryAdd()
    {
        // Arrange
        var request = new CreateFiscalPeriodDto
        {
            FiscalCalendarId = Guid.NewGuid(),
            PeriodName = "January 2024",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31),
            Description = "First period"
        };

        var expectedPeriod = new FiscalPeriod
        {
            Id = Guid.NewGuid(),
            FiscalCalendarId = request.FiscalCalendarId,
            PeriodName = request.PeriodName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description,
            IsClosed = false,
            CreatedByUserId = _userId
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<FiscalPeriod>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request, _userId);

        // Assert
        result.Should().NotBeNull();
        result.PeriodName.Should().Be("January 2024");
        result.IsClosed.Should().BeFalse();
        
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<FiscalPeriod>()), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenPeriodExists_ReturnsMappedDto()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var period = new FiscalPeriod
        {
            Id = periodId,
            CompanyId = Guid.NewGuid(),
            FiscalCalendarId = Guid.NewGuid(),
            PeriodName = "January",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31),
            IsClosed = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync(period);

        // Act
        var result = await _service.GetByIdAsync(periodId);

        // Assert
        result.Should().NotBeNull();
        result?.Id.Should().Be(periodId);
        result?.PeriodName.Should().Be("January");
    }

    [Fact]
    public async Task GetByIdAsync_WhenPeriodDoesNotExist_ReturnsNull()
    {
        // Arrange
        var periodId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync((FiscalPeriod?)null);

        // Act
        var result = await _service.GetByIdAsync(periodId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenPeriodIsDeleted_ReturnsNull()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var period = new FiscalPeriod
        {
            Id = periodId,
            IsDeleted = true,
            PeriodName = "Deleted Period"
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync(period);

        // Act
        var result = await _service.GetByIdAsync(periodId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByCalendarIdAsync Tests

    [Fact]
    public async Task GetByCalendarIdAsync_WhenPeriodsExist_ReturnsAllPeriods()
    {
        // Arrange
        var calendarId = Guid.NewGuid();
        var periods = new List<FiscalPeriod>
        {
            new FiscalPeriod
            {
                Id = Guid.NewGuid(),
                FiscalCalendarId = calendarId,
                PeriodName = "January",
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 1, 31),
                IsDeleted = false
            },
            new FiscalPeriod
            {
                Id = Guid.NewGuid(),
                FiscalCalendarId = calendarId,
                PeriodName = "February",
                StartDate = new DateTime(2024, 2, 1),
                EndDate = new DateTime(2024, 2, 29),
                IsDeleted = false
            }
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<FiscalPeriod, bool>>>(), It.IsAny<Func<IQueryable<FiscalPeriod>, IOrderedQueryable<FiscalPeriod>>>(), It.IsAny<Func<IQueryable<FiscalPeriod>, IQueryable<FiscalPeriod>>>()))
            .ReturnsAsync((periods.AsEnumerable(), periods.Count));

        // Act
        var results = await _service.GetByCalendarIdAsync(calendarId, new PaginationParams());

        // Assert
        results.Data.Should().HaveCount(2);
        results.Data.Select(p => p.PeriodName).Should().Contain(new[] { "January", "February" });
    }

    [Fact]
    public async Task GetByCalendarIdAsync_FiltersDeletedPeriods()
    {
        // Arrange
        var calendarId = Guid.NewGuid();
        var activePeriods = new List<FiscalPeriod>
        {
            new FiscalPeriod { Id = Guid.NewGuid(), FiscalCalendarId = calendarId, PeriodName = "January", IsDeleted = false }
        };

        // Repository returns only non-deleted records (EF Core global query filter)
        _repositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<FiscalPeriod, bool>>>(), It.IsAny<Func<IQueryable<FiscalPeriod>, IOrderedQueryable<FiscalPeriod>>>(), It.IsAny<Func<IQueryable<FiscalPeriod>, IQueryable<FiscalPeriod>>>()))
            .ReturnsAsync((activePeriods.AsEnumerable(), activePeriods.Count));

        // Act
        var results = await _service.GetByCalendarIdAsync(calendarId, new PaginationParams());

        // Assert
        results.Data.Should().HaveCount(1);
        results.Data.First().PeriodName.Should().Be("January");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenValidRequest_UpdatesPeriod()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var period = new FiscalPeriod
        {
            Id = periodId,
            PeriodName = "January",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31)
        };

        var updateRequest = new UpdateFiscalPeriodDto
        {
            PeriodName = "January Updated"
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync(period);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(periodId, updateRequest, _userId);

        // Assert
        result.Should().NotBeNull();
        result.PeriodName.Should().Be("January Updated");
        _repositoryMock.Verify(x => x.Update(It.IsAny<FiscalPeriod>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenPeriodNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var updateRequest = new UpdateFiscalPeriodDto { PeriodName = "Updated" };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync((FiscalPeriod?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(periodId, updateRequest, _userId));
    }

    #endregion

    #region CloseAsync Tests

    [Fact]
    public async Task CloseAsync_WhenValidId_ClosesPeriod()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var period = new FiscalPeriod
        {
            Id = periodId,
            PeriodName = "January",
            IsClosed = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync(period);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _service.CloseAsync(periodId, _userId);

        // Assert
        period.IsClosed.Should().BeTrue();
        period.ClosedByUserId.Should().Be(_userId);
        period.ClosedAt.Should().NotBeNull();
        
        _repositoryMock.Verify(x => x.Update(period), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CloseAsync_WhenAlreadyClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var period = new FiscalPeriod
        {
            Id = periodId,
            PeriodName = "January",
            IsClosed = true
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync(period);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CloseAsync(periodId, _userId));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenValidId_MarksPeriodAsDeleted()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var period = new FiscalPeriod
        {
            Id = periodId,
            PeriodName = "January",
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(periodId))
            .ReturnsAsync(period);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(periodId, _userId);

        // Assert
        period.IsDeleted.Should().BeTrue();
        period.DeletedByUserId.Should().Be(_userId);
        period.DeletedAt.Should().NotBeNull();
        
        _repositoryMock.Verify(x => x.Update(period), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    #endregion
}
