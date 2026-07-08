using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Infrastructure.Services;
using Nexcore.SharedKernel.Api;
using System.Linq.Expressions;

namespace Accounting.Tests.Unit.Services;

/// <summary>
/// Unit tests for LedgerService
/// Tests ledger creation, retrieval, updating, and deletion operations
/// </summary>
public class LedgerServiceTests
{
    private readonly Mock<ILedgerRepository> _repositoryMock;
    private readonly Mock<ILogger<LedgerService>> _loggerMock;
    private readonly ILedgerService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();

    public LedgerServiceTests()
    {
        _repositoryMock = new Mock<ILedgerRepository>();
        _loggerMock = new Mock<ILogger<LedgerService>>();
        _service = new LedgerService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesLedger()
    {
        // Arrange
        var request = new CreateLedgerDto
        {
            Name = "General Ledger",
            BaseCurrencyCode = "USD",
            FiscalCalendarId = Guid.NewGuid(),
            IsDefault = true,
            Description = "Default ledger"
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Ledger>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("General Ledger");
        result.BaseCurrencyCode.Should().Be("USD");
        result.IsActive.Should().BeTrue();
        
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Ledger>()), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenMultipleLedgersCreated_AllCreatedSuccessfully()
    {
        // Arrange
        var requests = new[]
        {
            new CreateLedgerDto { Name = "GL", BaseCurrencyCode = "USD", IsDefault = true },
            new CreateLedgerDto { Name = "Cost Ledger", BaseCurrencyCode = "USD", IsDefault = false }
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Ledger>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var results = new List<LedgerDto>();
        foreach (var request in requests)
        {
            var result = await _service.CreateAsync(request, _userId);
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Ledger>()), Times.Exactly(2));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenLedgerExists_ReturnsLedger()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var ledger = new Ledger
        {
            Id = ledgerId,
            CompanyId = _companyId,
            Name = "General Ledger",
            BaseCurrencyCode = "USD",
            IsActive = true,
            IsDefault = true
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync(ledger);

        // Act
        var result = await _service.GetByIdAsync(ledgerId);

        // Assert
        result.Should().NotBeNull();
        result?.Id.Should().Be(ledgerId);
        result?.Name.Should().Be("General Ledger");
    }

    [Fact]
    public async Task GetByIdAsync_WhenLedgerNotFound_ReturnsNull()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync((Ledger?)null);

        // Act
        var result = await _service.GetByIdAsync(ledgerId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenLedgerDeleted_ReturnsNull()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var ledger = new Ledger
        {
            Id = ledgerId,
            Name = "Deleted",
            BaseCurrencyCode = "USD",
            IsDeleted = true
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync(ledger);

        // Act
        var result = await _service.GetByIdAsync(ledgerId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenLedgersExist_ReturnsAllLedgers()
    {
        // Arrange
        var ledgers = new List<Ledger>
        {
            new Ledger { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "GL", BaseCurrencyCode = "USD", IsDeleted = false },
            new Ledger { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Cost Ledger", BaseCurrencyCode = "USD", IsDeleted = false }
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Ledger, bool>>>(), It.IsAny<Func<IQueryable<Ledger>, IOrderedQueryable<Ledger>>>(), It.IsAny<Func<IQueryable<Ledger>, IQueryable<Ledger>>>()))
            .ReturnsAsync((ledgers.AsEnumerable(), ledgers.Count));

        // Act
        var results = await _service.GetAllAsync(new PaginationParams());

        // Assert
        results.Data.Should().HaveCount(2);
        results.Data.Select(l => l.Name).Should().Contain(new[] { "GL", "Cost Ledger" });
    }

    [Fact]
    public async Task GetAllAsync_FiltersDeletedLedgers()
    {
        // Arrange
        var activeLedgers = new List<Ledger>
        {
            new Ledger { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "GL", BaseCurrencyCode = "USD", IsDeleted = false }
        };

        // Repository returns only non-deleted records (EF Core global query filter)
        _repositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Ledger, bool>>>(), It.IsAny<Func<IQueryable<Ledger>, IOrderedQueryable<Ledger>>>(), It.IsAny<Func<IQueryable<Ledger>, IQueryable<Ledger>>>()))
            .ReturnsAsync((activeLedgers.AsEnumerable(), activeLedgers.Count));

        // Act
        var results = await _service.GetAllAsync(new PaginationParams());

        // Assert
        results.Data.Should().HaveCount(1);
        results.Data.First().Name.Should().Be("GL");
    }

    #endregion

    #region GetDefaultAsync Tests

    [Fact]
    public async Task GetDefaultAsync_WhenDefaultExists_ReturnsDefault()
    {
        // Arrange
        var defaultLedger = new Ledger
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "Default Ledger",
            BaseCurrencyCode = "USD",
            IsDefault = true,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetDefaultLedgerAsync())
            .ReturnsAsync(defaultLedger);

        // Act
        var result = await _service.GetDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result?.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefaultAsync_WhenDefaultNotFound_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetDefaultLedgerAsync())
            .ReturnsAsync((Ledger?)null);

        // Act
        var result = await _service.GetDefaultAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenValidRequest_UpdatesLedger()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var existingLedger = new Ledger
        {
            Id = ledgerId,
            Name = "Old Name",
            BaseCurrencyCode = "USD",
            IsActive = true
        };

        var updateRequest = new UpdateLedgerDto
        {
            Name = "Updated Name",
            IsActive = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync(existingLedger);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(ledgerId, updateRequest, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.IsActive.Should().BeFalse();
        _repositoryMock.Verify(x => x.Update(It.IsAny<Ledger>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenLedgerNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var updateRequest = new UpdateLedgerDto { Name = "Updated" };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync((Ledger?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(ledgerId, updateRequest, _userId));
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var originalName = "Original Name";
        var existingLedger = new Ledger
        {
            Id = ledgerId,
            Name = originalName,
            BaseCurrencyCode = "USD"
        };

        var updateRequest = new UpdateLedgerDto { Name = null }; // Only update name to null

        _repositoryMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync(existingLedger);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(ledgerId, updateRequest, _userId);

        // Assert
        result.Name.Should().Be(originalName); // Should remain unchanged
        _repositoryMock.Verify(x => x.Update(It.IsAny<Ledger>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenLedgerExists_MarkAsDeleted()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var ledger = new Ledger
        {
            Id = ledgerId,
            Name = "Ledger to Delete",
            BaseCurrencyCode = "USD",
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync(ledger);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(ledgerId, _userId);

        // Assert
        ledger.IsDeleted.Should().BeTrue();
        ledger.DeletedByUserId.Should().Be(_userId);
        ledger.DeletedAt.Should().NotBeNull();
        _repositoryMock.Verify(x => x.Update(ledger), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenLedgerNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync((Ledger?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteAsync(ledgerId, _userId));
    }

    #endregion
}
