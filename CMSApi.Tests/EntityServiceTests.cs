using CMSApi.Data.Repository;
using CMSApi.Domain;
using CMSApi.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CMSApi.Tests;

public class EntityServiceTests
{
    private readonly Mock<ICmsEntityRepository> _mockRepo;
    private readonly Mock<ILogger<EntityService>> _mockLogger;
    private readonly EntityService _service;

    public EntityServiceTests()
    {
        _mockRepo = new Mock<ICmsEntityRepository>();
        _mockLogger = new Mock<ILogger<EntityService>>();
        _service = new EntityService(_mockRepo.Object, _mockLogger.Object);
    }

    private static Dictionary<string, CmsEntity> MakeEntities(params (string Id, bool IsDisabled)[] items)
    {
        return items.ToDictionary(i => i.Id, i => new CmsEntity { Id = i.Id, IsDisabled = i.IsDisabled });
    }

    private void VerifyDisabledEntity(string id)
    {
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetEnabledEntitiesAsync_ShouldReturnOnlyEnabled()
    {
        var entities = MakeEntities(("1", false), ("2", true), ("3", false));
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);

        var result = await _service.GetEnabledEntitiesAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.False(e.IsDisabled));
    }

    [Fact]
    public async Task GetAllEntitiesAsync_ShouldReturnAll()
    {
        var entities = MakeEntities(("1", false), ("2", true));
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);

        var result = await _service.GetAllEntitiesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == "1");
        Assert.Contains(result, e => e.Id == "2");
    }

    [Fact]
    public async Task DisableEntityAsync_ShouldSetIsDisabledTrue()
    {
        var entity = new CmsEntity { Id = "1", IsDisabled = false };
        _mockRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                 .ReturnsAsync(new Dictionary<string, CmsEntity> { { "1", entity } });

        await _service.DisableEntityAsync("1");

        Assert.True(entity.IsDisabled);
        VerifyDisabledEntity("1");
    }

    [Fact]
    public async Task DisableEntityAsync_ShouldThrowIfNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                 .ReturnsAsync(new Dictionary<string, CmsEntity>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DisableEntityAsync("missing"));
    }

    [Fact]
    public async Task GetEnabledEntitiesAsync_ShouldReturnEmpty_WhenNoEnabled()
    {
        var entities = MakeEntities(("1", true));
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);

        var result = await _service.GetEnabledEntitiesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEnabledEntitiesAsync_ShouldReturnEmpty_WhenNoEntities()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetEnabledEntitiesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task DisableEntityAsync_ShouldThrow_WhenIdIsNullOrEmpty()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.DisableEntityAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.DisableEntityAsync(string.Empty));
    }

    [Fact]
    public async Task DisableEntityAsync_ShouldThrow_WhenIdIsWhitespace()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.DisableEntityAsync("  "));
    }

    [Fact]
    public async Task DisableEntityAsync_ShouldNotCallSave_WhenEntityAlreadyDisabled()
    {
        var entity = new CmsEntity { Id = "1", IsDisabled = true };
        _mockRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                       .ReturnsAsync(new Dictionary<string, CmsEntity> { ["1"] = entity });

        await _service.DisableEntityAsync("1");

        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}