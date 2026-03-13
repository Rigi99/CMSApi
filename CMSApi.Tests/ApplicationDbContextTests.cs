using CMSApi.Data;
using CMSApi.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CMSApi.Tests;

public class ApplicationDbContextTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Can_Add_And_Retrieve_CmsEntity()
    {
        await using var context = CreateContext();

        var entity = new CmsEntity { Id = "1" };
        await context.CmsEntities.AddAsync(entity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fetched = await context.CmsEntities.FindAsync(["1"], TestContext.Current.CancellationToken);
        Assert.NotNull(fetched);
        Assert.Equal("1", fetched!.Id);
    }

    [Fact]
    public async Task Can_Add_And_Retrieve_CmsEntityVersion()
    {
        await using var context = CreateContext();

        var entity = new CmsEntity { Id = "1" };
        var version = new CmsEntityVersion
        {
            CmsEntityId = "1",
            Version = 1,
            Timestamp = DateTime.UtcNow
        };
        entity.Versions.Add(version);

        await context.CmsEntities.AddAsync(entity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fetchedEntity = await context.CmsEntities
            .Include(e => e.Versions)
            .FirstOrDefaultAsync(e => e.Id == "1", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(fetchedEntity);
        Assert.Single(fetchedEntity!.Versions);
        Assert.Equal(1, fetchedEntity.Versions.First().Version);
    }

    [Fact]
    public async Task Cascade_Delete_Removes_Versions()
    {
        await using var context = CreateContext();

        var entity = new CmsEntity { Id = "1" };
        var version = new CmsEntityVersion
        {
            CmsEntityId = "1",
            Version = 1,
            Timestamp = DateTime.UtcNow
        };
        entity.Versions.Add(version);

        await context.CmsEntities.AddAsync(entity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.CmsEntities.Remove(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var versionsCount = await context.CmsEntityVersions.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, versionsCount);
    }
}