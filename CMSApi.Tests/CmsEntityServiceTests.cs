using CMSApi.Data.Repository;
using CMSApi.Domain;
using CMSApi.Dtos;
using CMSApi.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CMSApi.Tests;

public class CmsEntityServiceTests
{
    private readonly Mock<ICmsEntityRepository> _entityRepo;
    private readonly Mock<ICmsEntityVersionRepository> _versionRepo;
    private readonly Mock<ILogger<CmsEntityService>> _logger;
    private readonly CmsEntityService _service;

    public CmsEntityServiceTests()
    {
        _entityRepo = new Mock<ICmsEntityRepository>();
        _versionRepo = new Mock<ICmsEntityVersionRepository>();
        _logger = new Mock<ILogger<CmsEntityService>>();
        _service = new CmsEntityService(_entityRepo.Object, _versionRepo.Object, _logger.Object);
    }

    #region Helpers

    private static CmsEntityDto MakeEvent(
        string id,
        string type = "publish",
        int version = 1,
        DateTime? timestamp = null)
        => new() { Id = id, Type = type, Version = version, Timestamp = timestamp ?? DateTime.UtcNow };

    private Mock<IDbContextTransaction> SetupTransactionForTest()
    {
        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(t => t.CommitAsync(default)).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.RollbackAsync(default)).Returns(Task.CompletedTask);
        _entityRepo.Setup(r => r.BeginTransactionAsync())
                   .ReturnsAsync(transactionMock.Object);
        return transactionMock;
    }

    private void SetupEntities(Dictionary<string, CmsEntity>? entities = null)
    {
        _entityRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                   .ReturnsAsync(entities ?? new Dictionary<string, CmsEntity>());
    }

    private void SetupRepoMethods()
    {
        _entityRepo.Setup(r => r.AddAsync(It.IsAny<CmsEntity>())).Returns(Task.CompletedTask);
        _entityRepo.Setup(r => r.RemoveAsync(It.IsAny<CmsEntity>())).Returns(Task.CompletedTask);
        _entityRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        _versionRepo.Setup(r => r.AddVersionAsync(It.IsAny<CmsEntity>(), It.IsAny<CmsEntityDto>()))
                    .Returns(Task.CompletedTask);
    }

    private void SetupRepositoryMocks(Dictionary<string, CmsEntity>? entities = null)
    {
        SetupEntities(entities);
        SetupRepoMethods();
        SetupTransactionForTest();
    }

    // Changed 'Times times' -> 'int times' for simplicity
    private void VerifyLoggerWarningContains(string text, int times = 1)
    {
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(text)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Moq.Times.Exactly(times));
    }

    #endregion

    #region Tests

    [Fact]
    public async Task ProcessEventsAsync_ShouldAddPublishEvent()
    {
        var evt = MakeEvent("1", "publish");
        SetupRepositoryMocks();

        await _service.ProcessEventsAsync(new[] { evt });

        _entityRepo.Verify(r => r.AddAsync(It.Is<CmsEntity>(e => e.Id == "1")), Times.Once);
        _versionRepo.Verify(r => r.AddVersionAsync(It.IsAny<CmsEntity>(), evt), Times.Once);
    }

    [Fact]
    public async Task ProcessEventsAsync_ShouldUpdateExistingEntity()
    {
        var entity = new CmsEntity { Id = "1", LatestVersion = 1 };
        var evt = MakeEvent("1", "update", 2);

        SetupRepositoryMocks(new Dictionary<string, CmsEntity> { { "1", entity } });

        await _service.ProcessEventsAsync(new[] { evt });

        Assert.Equal(2, entity.LatestVersion);
    }

    [Fact]
    public async Task ProcessEventsAsync_ShouldDisableEntityOnUnpublish()
    {
        var entity = new CmsEntity { Id = "1", IsDisabled = false };
        var evt = MakeEvent("1", "unpublish");

        SetupRepositoryMocks(new Dictionary<string, CmsEntity> { { "1", entity } });

        await _service.ProcessEventsAsync(new[] { evt });

        Assert.True(entity.IsDisabled);
        _versionRepo.Verify(v => v.AddVersionAsync(entity, evt), Times.Once);
    }

    [Fact]
    public async Task ProcessEventsAsync_ShouldDeleteEntity()
    {
        var entity = new CmsEntity { Id = "1" };
        var dict = new Dictionary<string, CmsEntity> { { "1", entity } };
        var evt = MakeEvent("1", "delete");

        SetupRepositoryMocks(dict);

        await _service.ProcessEventsAsync(new[] { evt });

        _entityRepo.Verify(r => r.RemoveAsync(entity), Times.Once);
        Assert.False(dict.ContainsKey("1"));
    }

    [Fact]
    public async Task ProcessEventsAsync_ShouldThrowOnInvalidEvent()
    {
        var evt = MakeEvent("", "publish");
        SetupRepositoryMocks();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ProcessEventsAsync(new[] { evt }));
    }

    [Fact]
    public async Task ProcessEventsAsync_ShouldThrow_OnEmptyEventList()
    {
        SetupRepositoryMocks();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ProcessEventsAsync(Array.Empty<CmsEntityDto>()));
    }

    [Fact]
    public async Task ProcessEventsAsync_ShouldThrow_OnUnknownEventType()
    {
        var evt = MakeEvent("1", "foobar");
        SetupRepositoryMocks();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ProcessEventsAsync(new[] { evt }));
    }

    [Fact]
    public async Task ProcessEventsAsync_ShouldRollbackTransaction_OnException()
    {
        var evt = MakeEvent("1", "publish");

        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(t => t.RollbackAsync(default)).Returns(Task.CompletedTask);
        _entityRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
        _entityRepo.Setup(r => r.AddAsync(It.IsAny<CmsEntity>())).ThrowsAsync(new Exception("DB error"));

        await Assert.ThrowsAsync<Exception>(() => _service.ProcessEventsAsync(new[] { evt }));

        transactionMock.Verify(t => t.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task ProcessEventsAsync_ShouldThrow_WhenEventsIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ProcessEventsAsync(null!));
    }

    [Fact]
    public async Task ProcessEventsAsync_ShouldAddEntity_WhenEntityNotExists()
    {
        var dto = MakeEvent("new", "publish");
        SetupRepositoryMocks();

        await _service.ProcessEventsAsync(new[] { dto });

        _entityRepo.Verify(r => r.AddAsync(It.IsAny<CmsEntity>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEventsAsync_Delete_ShouldHandleNullEntityGracefully()
    {
        var dto = MakeEvent("missing", "delete");
        SetupRepositoryMocks();

        await _service.ProcessEventsAsync(new[] { dto });

        _entityRepo.Verify(r => r.RemoveAsync(It.IsAny<CmsEntity>()), Times.Never);
        VerifyLoggerWarningContains("Delete event for missing entity", 1); 
    }

    #endregion
}