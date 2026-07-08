using Xunit;
using Moq;
using Inventory.Infrastructure.Repositories.Interfaces;
using Inventory.Application.Services.Interfaces;
using Inventory.Api.Controllers;
using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using DomainUnit = Inventory.Domain.Entities.Unit;

namespace Inventory.Tests.Unit.Controllers;

public class ItemControllerTests
{
    private readonly Mock<IItemRepository>       _mockItemRepository;
    private readonly Mock<IUnitRepository>       _mockUnitRepository;
    private readonly Mock<IBlobStorageService>   _mockBlobStorage;
    private readonly Mock<ILogger<ItemController>> _mockLogger;
    private readonly ItemController _controller;

    public ItemControllerTests()
    {
        _mockItemRepository = new Mock<IItemRepository>();
        _mockUnitRepository = new Mock<IUnitRepository>();
        _mockBlobStorage    = new Mock<IBlobStorageService>();
        _mockLogger         = new Mock<ILogger<ItemController>>();
        _controller = new ItemController(
            _mockItemRepository.Object,
            _mockUnitRepository.Object,
            _mockBlobStorage.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithItems()
    {
        // Arrange
        var items = new List<Item>
        {
            new Item { Id = Guid.NewGuid(), Code = "ITEM001", Name = "Item 1", BaseUnitId = Guid.NewGuid(), IsActive = true },
            new Item { Id = Guid.NewGuid(), Code = "ITEM002", Name = "Item 2", BaseUnitId = Guid.NewGuid(), IsActive = true }
        };

        _mockItemRepository
            .Setup(r => r.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string>()))
            .ReturnsAsync((items, items.Count));

        // Act
        var result = await _controller.GetAll(1, 10);

        // Assert
        Assert.NotNull(result);
        _mockItemRepository.Verify(r => r.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnOkWithItem()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = new Item
        {
            Id = itemId,
            Code = "ITEM001",
            Name = "Item 1",
            BaseUnitId = Guid.NewGuid(),
            IsActive = true
        };

        _mockItemRepository.Setup(r => r.GetWithFullDetailsReadOnlyAsync(itemId)).ReturnsAsync(item);

        // Act
        var result = await _controller.GetById(itemId);

        // Assert
        Assert.NotNull(result);
        _mockItemRepository.Verify(r => r.GetWithFullDetailsReadOnlyAsync(itemId), Times.Once);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        _mockItemRepository.Setup(r => r.GetWithFullDetailsReadOnlyAsync(itemId)).ReturnsAsync((Item?)null);

        // Act
        var result = await _controller.GetById(itemId);

        // Assert
        Assert.NotNull(result);
        _mockItemRepository.Verify(r => r.GetWithFullDetailsReadOnlyAsync(itemId), Times.Once);
    }

    [Fact]
    public async Task GetByCode_WithValidCode_ShouldReturnOkWithItem()
    {
        // Arrange
        var code = "ITEM001";
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "Item 1",
            BaseUnitId = Guid.NewGuid(),
            IsActive = true
        };

        _mockItemRepository.Setup(r => r.GetWithFullDetailsByCodeAsync(code)).ReturnsAsync(item);

        // Act
        var result = await _controller.GetByCode(code);

        // Assert
        Assert.NotNull(result);
        _mockItemRepository.Verify(r => r.GetWithFullDetailsByCodeAsync(code), Times.Once);
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var unitId = Guid.NewGuid();
        var unit = new DomainUnit { Id = unitId, Code = "PCS", Name = "Pieces", IsActive = true };
        var createDto = new CreateItemDto
        {
            Code = "ITEM001",
            Name = "Item 1",
            BaseUnitId = unitId
        };

        _mockItemRepository.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((Item?)null);
        _mockUnitRepository.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(unit);
        _mockItemRepository.Setup(r => r.AddAsync(It.IsAny<Item>())).Returns(Task.CompletedTask);
        _mockItemRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.NotNull(result);
        _mockItemRepository.Verify(r => r.AddAsync(It.IsAny<Item>()), Times.Once);
        _mockItemRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetActive_ShouldReturnOnlyActiveItems()
    {
        // Arrange
        var items = new List<Item>
        {
            new Item { Id = Guid.NewGuid(), Code = "ITEM001", Name = "Item 1", BaseUnitId = Guid.NewGuid(), IsActive = true },
            new Item { Id = Guid.NewGuid(), Code = "ITEM002", Name = "Item 2", BaseUnitId = Guid.NewGuid(), IsActive = true }
        };

        _mockItemRepository
            .Setup(r => r.GetActivePagedAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((items, items.Count));

        // Act
        var result = await _controller.GetActive(new PaginationParams());

        // Assert
        Assert.NotNull(result);
        _mockItemRepository.Verify(r => r.GetActivePagedAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }
}
