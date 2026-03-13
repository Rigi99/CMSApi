using CMSApi.Data;
using CMSApi.Data.Repository;
using CMSApi.Domain;
using CMSApi.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CMSApi.Tests;

public class CmsEntityVersionRepositoryTests
{
    private readonly CmsEntityVersionRepository _repo;
    private readonly ApplicationDbContext _db;

    public CmsEntityVersionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        var loggerMock = new Mock<ILogger<CmsEntityVersionRepository>>();
        _repo = new CmsEntityVersionRepository(_db, loggerMock.Object);
    }

    [Fact]
    public async Task AddVersionAsync_ShouldAddNewVersion()
    {
        var entity = new CmsEntity { Id = "1" };
        var dto = new CmsEntityDto { Id = "1", Version = 1, Type = "publish", Timestamp = DateTime.UtcNow };

        await _repo.AddVersionAsync(entity, dto);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Single(entity.Versions);
        var version = entity.Versions.First();
        Assert.Equal(1, version.Version);
        Assert.Equal("1", version.CmsEntityId);
    }

    [Fact]
    public async Task AddVersionAsync_ShouldNotAddDuplicateVersion()
    {
        var entity = new CmsEntity { Id = "1" };
        var dto = new CmsEntityDto { Id = "1", Version = 1, Type = "publish", Timestamp = DateTime.UtcNow };

        await _repo.AddVersionAsync(entity, dto);
        await _repo.AddVersionAsync(entity, dto); // duplicate

        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Single(entity.Versions);
    }

    [Fact]
    public async Task GetByEntityIdAsync_ShouldReturnVersionsOrdered()
    {
        var v1 = new CmsEntityVersion { CmsEntityId = "1", Version = 1, Timestamp = DateTime.UtcNow };
        var v2 = new CmsEntityVersion { CmsEntityId = "1", Version = 2, Timestamp = DateTime.UtcNow.AddMinutes(1) };

        await _repo.AddAsync(v2);
        await _repo.AddAsync(v1);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repo.GetByEntityIdAsync("1");

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Version);
        Assert.Equal(2, result[1].Version);
    }

    [Fact]
    public async Task AddAsync_ShouldAddToDbContext()
    {
        var version = new CmsEntityVersion
        {
            CmsEntityId = "1",
            Version = 1,
            Timestamp = DateTime.UtcNow
        };

        await _repo.AddAsync(version);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _db.CmsEntityVersions.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("1", result[0].CmsEntityId);
        Assert.Equal(1, result[0].Version);
    }
}