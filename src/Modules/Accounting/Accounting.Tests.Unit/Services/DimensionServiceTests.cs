using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Infrastructure.Services;
using System.Linq.Expressions;

namespace Accounting.Tests.Unit.Services;

/// <summary>
/// Unit tests for DimensionService
/// Tests analytical dimension management
/// </summary>
public class DimensionServiceTests
{
    private readonly Mock<IDimensionRepository> _repositoryMock;
    private readonly Mock<IDimensionValueRepository> _valueRepoMock;
    private readonly Mock<ILogger<DimensionService>> _loggerMock;
    private readonly IDimensionService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public DimensionServiceTests()
    {
        _repositoryMock = new Mock<IDimensionRepository>();
        _valueRepoMock = new Mock<IDimensionValueRepository>();
        _loggerMock = new Mock<ILogger<DimensionService>>();
        _service = new DimensionService(_repositoryMock.Object, _valueRepoMock.Object, _loggerMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesDimension()
    {
        // Arrange
        var request = new CreateDimensionDto
        {
            Code = "CC",
            Name = "Cost Center"
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Dimension>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("CC");
        result.Name.Should().Be("Cost Center");
    }

    #endregion

    #region GetByCodeAsync Tests

    [Fact]
    public async Task GetByCodeAsync_WhenDimensionExists_ReturnsDimension()
    {
        // Arrange
        var code = "CC";
        var dimension = new Dimension
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "Cost Center"
        };

        _repositoryMock
            .Setup(x => x.GetByCodeAsync(code))
            .ReturnsAsync(dimension);

        // Act
        var result = await _service.GetByCodeAsync(code);

        // Assert
        result.Should().NotBeNull();
        result?.Code.Should().Be(code);
    }

    #endregion
}
