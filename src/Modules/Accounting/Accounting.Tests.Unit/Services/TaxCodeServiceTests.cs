using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Infrastructure.Services;
using Nexcore.SharedKernel.Api;
using System.Linq.Expressions;

namespace Accounting.Tests.Unit.Services;

/// <summary>
/// Unit tests for TaxCodeService
/// Tests tax code retrieval and filtering operations
/// </summary>
public class TaxCodeServiceTests
{
    private readonly Mock<ITaxCodeRepository> _repositoryMock;
    private readonly Mock<ILogger<TaxCodeService>> _loggerMock;
    private readonly ITaxCodeService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public TaxCodeServiceTests()
    {
        _repositoryMock = new Mock<ITaxCodeRepository>();
        _loggerMock = new Mock<ILogger<TaxCodeService>>();
        _service = new TaxCodeService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenTaxCodesExist_ReturnsAll()
    {
        // Arrange
        var taxCodes = new List<TaxCode>
        {
            new TaxCode { Id = Guid.NewGuid(), Code = "VAT15", Name = "VAT 15%", Percentage = 15.0m, IsDeleted = false },
            new TaxCode { Id = Guid.NewGuid(), Code = "VAT20", Name = "VAT 20%", Percentage = 20.0m, IsDeleted = false }
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<TaxCode, bool>>>(), It.IsAny<Func<IQueryable<TaxCode>, IOrderedQueryable<TaxCode>>>(), It.IsAny<Func<IQueryable<TaxCode>, IQueryable<TaxCode>>>()))
            .ReturnsAsync((taxCodes.AsEnumerable(), taxCodes.Count));

        // Act
        var results = await _service.GetAllAsync(new PaginationParams());

        // Assert
        results.Data.Should().HaveCount(2);
        results.Data.Select(t => t.Code).Should().Contain(new[] { "VAT15", "VAT20" });
    }

    [Fact]
    public async Task GetAllAsync_WhenNoTaxCodesExist_ReturnsEmpty()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<TaxCode, bool>>>(), It.IsAny<Func<IQueryable<TaxCode>, IOrderedQueryable<TaxCode>>>(), It.IsAny<Func<IQueryable<TaxCode>, IQueryable<TaxCode>>>()))
            .ReturnsAsync((Enumerable.Empty<TaxCode>(), 0));

        // Act
        var results = await _service.GetAllAsync(new PaginationParams());

        // Assert
        results.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResponse()
    {
        // Arrange
        var taxCodes = new List<TaxCode>
        {
            new TaxCode { Id = Guid.NewGuid(), Code = "VAT15", Name = "VAT 15%", Percentage = 15.0m, IsDeleted = false }
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<TaxCode, bool>>>(), It.IsAny<Func<IQueryable<TaxCode>, IOrderedQueryable<TaxCode>>>(), It.IsAny<Func<IQueryable<TaxCode>, IQueryable<TaxCode>>>()))
            .ReturnsAsync((taxCodes.AsEnumerable(), taxCodes.Count));

        // Act
        var results = await _service.GetAllAsync(new PaginationParams());

        // Assert
        results.Success.Should().BeTrue();
        results.Data.Should().HaveCount(1);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenTaxCodeExists_ReturnsTaxCode()
    {
        // Arrange
        var taxCodeId = Guid.NewGuid();
        var taxCode = new TaxCode
        {
            Id = taxCodeId,
            Code = "VAT15",
            Name = "VAT 15%",
            Percentage = 15.0m,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(taxCodeId))
            .ReturnsAsync(taxCode);

        // Act
        var result = await _service.GetByIdAsync(taxCodeId);

        // Assert
        result.Should().NotBeNull();
        result?.Code.Should().Be("VAT15");
        result?.Percentage.Should().Be(15.0m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaxCodeNotFound_ReturnsNull()
    {
        // Arrange
        var taxCodeId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(taxCodeId))
            .ReturnsAsync((TaxCode?)null);

        // Act
        var result = await _service.GetByIdAsync(taxCodeId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaxCodeDeleted_ReturnsNull()
    {
        // Arrange
        var taxCodeId = Guid.NewGuid();
        var taxCode = new TaxCode
        {
            Id = taxCodeId,
            Code = "VAT15",
            Name = "VAT 15%",
            Percentage = 15.0m,
            IsDeleted = true
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(taxCodeId))
            .ReturnsAsync(taxCode);

        // Act
        var result = await _service.GetByIdAsync(taxCodeId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByCodeAsync Tests

    [Fact]
    public async Task GetByCodeAsync_WhenTaxCodeExists_ReturnsTaxCode()
    {
        // Arrange
        var code = "VAT15";
        var taxCode = new TaxCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "VAT 15%",
            Percentage = 15.0m,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetByCodeAsync(code))
            .ReturnsAsync(taxCode);

        // Act
        var result = await _service.GetByCodeAsync(code);

        // Assert
        result.Should().NotBeNull();
        result?.Code.Should().Be(code);
        result?.Percentage.Should().Be(15.0m);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenTaxCodeNotFound_ReturnsNull()
    {
        // Arrange
        var code = "NONEXISTENT";

        _repositoryMock
            .Setup(x => x.GetByCodeAsync(code))
            .ReturnsAsync((TaxCode?)null);

        // Act
        var result = await _service.GetByCodeAsync(code);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCodeAsync_WhenTaxCodeDeleted_ReturnsNull()
    {
        // Arrange
        var code = "VAT15";
        var taxCode = new TaxCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "VAT 15%",
            Percentage = 15.0m,
            IsDeleted = true
        };

        _repositoryMock
            .Setup(x => x.GetByCodeAsync(code))
            .ReturnsAsync(taxCode);

        // Act
        var result = await _service.GetByCodeAsync(code);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
