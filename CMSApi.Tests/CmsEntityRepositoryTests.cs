using CMSApi.Data;
using CMSApi.Data.Repository;
using CMSApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CMSApi.Tests.Repositories
{
    public class CmsEntityRepositoryTests
    {
        private readonly CmsEntityRepository _repo;
        private readonly ApplicationDbContext _db;
        private readonly ReadOnlyApplicationDbContext _readDb;

        public CmsEntityRepositoryTests()
        {
            var dbName = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            var readOptions = new DbContextOptionsBuilder<ReadOnlyApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            _db = new ApplicationDbContext(options);
            _readDb = new ReadOnlyApplicationDbContext(readOptions);

            var loggerMock = new Mock<ILogger<CmsEntityRepository>>();
            _repo = new CmsEntityRepository(_db, _readDb, loggerMock.Object);
        }

        [Fact]
        public async Task AddAsync_ShouldAddEntity()
        {
            var entity = new CmsEntity { Id = "1" };

            await _repo.AddAsync(entity);
            await _db.SaveChangesAsync(CancellationToken.None);

            var saved = await _db.CmsEntities.FindAsync(["1"], CancellationToken.None);
            Assert.NotNull(saved);
            Assert.Equal("1", saved.Id);
        }

        [Fact]
        public async Task GetByIdsAsync_ShouldReturnCorrectEntities()
        {
            var e1 = new CmsEntity { Id = "1" };
            var e2 = new CmsEntity { Id = "2" };
            await _db.CmsEntities.AddRangeAsync(e1, e2);
            await _db.SaveChangesAsync(CancellationToken.None);

            var result = await _repo.GetByIdsAsync(["1", "2", "3"]);

            Assert.Equal(2, result.Count);
            Assert.True(result.ContainsKey("1"));
            Assert.True(result.ContainsKey("2"));
            Assert.False(result.ContainsKey("3"));
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllEntities()
        {
            var e1 = new CmsEntity { Id = "1" };
            var e2 = new CmsEntity { Id = "2" };
            await _db.CmsEntities.AddRangeAsync(e1, e2);
            await _db.SaveChangesAsync(CancellationToken.None);

            var result = await _repo.GetAllAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, kv => kv.Key == "1");
            Assert.Contains(result, kv => kv.Key == "2");
        }

        [Fact]
        public async Task RemoveAsync_ShouldDeleteEntity()
        {
            var entity = new CmsEntity { Id = "1" };
            await _db.CmsEntities.AddAsync(entity, CancellationToken.None);
            await _db.SaveChangesAsync(CancellationToken.None);

            await _repo.RemoveAsync(entity);
            await _db.SaveChangesAsync(CancellationToken.None);

            var found = await _db.CmsEntities.FindAsync(new object?[] { "1" }, CancellationToken.None);
            Assert.Null(found);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldPersistChanges()
        {
            var entity = new CmsEntity { Id = "1" };
            await _repo.AddAsync(entity);

            var result = await _repo.SaveChangesAsync();

            Assert.Equal(1, result); 
        }
    }
}