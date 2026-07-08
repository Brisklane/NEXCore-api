using Xunit;
using Moq;
using Inventory.Infrastructure.Repositories.Interfaces;
using Inventory.Api.Controllers;
using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using System.Linq.Expressions;

namespace Inventory.Tests.Unit.Controllers;

public class WarehouseControllerTests
{
    private readonly Mock<IWarehouseRepository> _mockWarehouseRepository;
    private readonly Mock<IBinRepository> _mockBinRepository;
    private readonly Mock<ILogger<WarehouseController>> _mockLogger;
    private readonly WarehouseController _controller;

    public WarehouseControllerTests()
    {
        _mockWarehouseRepository = new Mock<IWarehouseRepository>();
        _mockBinRepository = new Mock<IBinRepository>();
        _mockLogger = new Mock<ILogger<WarehouseController>>();
        _controller = new WarehouseController(_mockWarehouseRepository.Object, _mockBinRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithWarehouses()
    {
        // Arrange
        var warehouses = new List<Warehouse>
        {
            new Warehouse { Id = Guid.NewGuid(), Code = "WH001", Name = "Main Warehouse", IsActive = true },
            new Warehouse { Id = Guid.NewGuid(), Code = "WH002", Name = "Secondary Warehouse", IsActive = true }
        };

        _mockWarehouseRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<Func<IQueryable<Warehouse>, IOrderedQueryable<Warehouse>>>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>>()))
            .ReturnsAsync((warehouses.AsEnumerable(), warehouses.Count));

        // Act
        var result = await _controller.GetAll(new PaginationParams(), null);

        // Assert
        Assert.NotNull(result);
        _mockWarehouseRepository.Verify(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<Func<IQueryable<Warehouse>, IOrderedQueryable<Warehouse>>>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnOkWithWarehouse()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = "WH001",
            Name = "Main Warehouse",
            IsActive = true
        };

        _mockWarehouseRepository.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync(warehouse);

        // Act
        var result = await _controller.GetById(warehouseId);

        // Assert
        Assert.NotNull(result);
        _mockWarehouseRepository.Verify(r => r.GetByIdAsync(warehouseId), Times.Once);
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateWarehouseDto
        {
            Code = "WH001",
            Name = "Main Warehouse",
            Address = "123 Main St",
            City = "New York",
            WarehouseType = "Main"
        };

        _mockWarehouseRepository.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((Warehouse?)null);
        _mockWarehouseRepository.Setup(r => r.AddAsync(It.IsAny<Warehouse>())).Returns(Task.CompletedTask);
        _mockWarehouseRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.NotNull(result);
        _mockWarehouseRepository.Verify(r => r.AddAsync(It.IsAny<Warehouse>()), Times.Once);
        _mockWarehouseRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetActive_ShouldReturnOnlyActiveWarehouses()
    {
        // Arrange
        var warehouses = new List<Warehouse>
        {
            new Warehouse { Id = Guid.NewGuid(), Code = "WH001", Name = "Main Warehouse", IsActive = true },
            new Warehouse { Id = Guid.NewGuid(), Code = "WH002", Name = "Secondary Warehouse", IsActive = true }
        };

        _mockWarehouseRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<Func<IQueryable<Warehouse>, IOrderedQueryable<Warehouse>>>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>>()))
            .ReturnsAsync((warehouses.AsEnumerable(), warehouses.Count));

        // Act
        var result = await _controller.GetActive(new PaginationParams());

        // Assert
        Assert.NotNull(result);
        _mockWarehouseRepository.Verify(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<Func<IQueryable<Warehouse>, IOrderedQueryable<Warehouse>>>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>>()), Times.Once);
    }
}
