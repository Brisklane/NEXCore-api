using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Infrastructure.Services;

namespace Accounting.Tests.Unit.Services;

/// <summary>
/// Unit tests for JournalEntryService
/// Tests journal entry operations and posting
/// </summary>
public class JournalEntryServiceTests
{
    private readonly Mock<IJournalEntryRepository> _repositoryMock;
    private readonly Mock<IJournalLineRepository> _lineRepositoryMock;
    private readonly Mock<ILogger<JournalEntryService>> _loggerMock;
    private readonly IJournalEntryService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public JournalEntryServiceTests()
    {
        _repositoryMock = new Mock<IJournalEntryRepository>();
        _lineRepositoryMock = new Mock<IJournalLineRepository>();
        _loggerMock = new Mock<ILogger<JournalEntryService>>();
        _service = new JournalEntryService(_repositoryMock.Object, _lineRepositoryMock.Object, _loggerMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesJournalEntry()
    {
        // Arrange
        var request = new CreateJournalEntryDto
        {
            DocumentType = "JE",
            Description = "Test Entry",
            CurrencyCode = "USD"
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<JournalEntry>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request, _userId);

        // Assert
        result.Should().NotBeNull();
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<JournalEntry>()), Times.Once);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenEntryExists_ReturnsEntry()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var entry = new JournalEntry
        {
            Id = entryId,
            JournalNumber = "JE001",
            CurrencyCode = "USD",
            Description = "Test",
            DocumentType = "JE",
            Status = JournalEntryStatus.Draft
        };

        _repositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(entryId))
            .ReturnsAsync(entry);

        // Act
        var result = await _service.GetByIdAsync(entryId);

        // Assert
        result.Should().NotBeNull();
        result?.JournalNumber.Should().Be("JE001");
    }

    #endregion
}
