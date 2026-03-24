using CMSApi.Controllers;
using CMSApi.Domain;
using CMSApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CMSApi.Tests;

public class EntityControllerTests
{
    private readonly Mock<IEntityService> _serviceMock;
    private readonly Mock<ILogger<EntityController>> _loggerMock;
    private readonly EntityController _controller;

    public EntityControllerTests()
    {
        _serviceMock = new Mock<IEntityService>();
        _loggerMock = new Mock<ILogger<EntityController>>();
        _controller = new EntityController(_serviceMock.Object, _loggerMock.Object);
    }

    private void SetUser(string username, string role = "ApiUser")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        }, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // ===== GET /entities =====
    [Fact]
    public async Task GetEntities_ShouldReturnOkWithEntities()
    {
        var entities = new List<CmsEntity>
        {
            new() { Id = "1" },
            new() { Id = "2" }
        };

        _serviceMock.Setup(s => s.GetEnabledEntitiesAsync()).ReturnsAsync(entities);

        SetUser("testuser");

        var result = await _controller.GetEntities();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnEntities = Assert.IsAssignableFrom<IEnumerable<CmsEntity>>(okResult.Value);
        Assert.Equal(2, returnEntities.Count());
    }

    [Fact]
    public async Task GetEntities_ShouldReturnEmpty_WhenNoEntitiesEnabled()
    {
        _serviceMock.Setup(s => s.GetEnabledEntitiesAsync()).ReturnsAsync(new List<CmsEntity>());

        SetUser("testuser");

        var result = await _controller.GetEntities();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<CmsEntity>>(okResult.Value);
        Assert.Empty(list);
    }

    // ===== GET /entities/admin =====
    [Fact]
    public async Task GetAllEntities_AsAdmin_ShouldReturnOk()
    {
        SetUser("admin_user", "Admin");

        var entities = new List<CmsEntity> { new() { Id = "1" } };
        _serviceMock.Setup(s => s.GetAllEntitiesAsync()).ReturnsAsync(entities);

        var result = await _controller.GetAllEntities();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnEntities = Assert.IsAssignableFrom<IEnumerable<CmsEntity>>(okResult.Value);
        Assert.Single(returnEntities);
    }

    [Fact]
    public async Task GetAllEntities_AsNonAdmin_ShouldReturnForbid()
    {
        SetUser("basic_user", "ApiUser");

        var result = await _controller.GetAllEntities();

        Assert.IsType<ForbidResult>(result);
    }

    // ===== PATCH /entities/{id}/disable =====
    [Fact]
    public async Task DisableEntity_AsAdmin_ShouldReturnOk()
    {
        SetUser("admin_user", "Admin");
        var id = "1";

        var result = await _controller.DisableEntity(id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.Contains(id, okResult.Value!.ToString());

        _serviceMock.Verify(s => s.DisableEntityAsync(id), Times.Once);
    }

    [Fact]
    public async Task DisableEntity_AsAdmin_NotFound_ShouldReturnNotFound()
    {
        SetUser("admin_user", "Admin");
        var id = "missing";

        _serviceMock.Setup(s => s.DisableEntityAsync(id))
                    .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.DisableEntity(id);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
        Assert.Contains("not found", notFoundResult.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DisableEntity_AsNonAdmin_ShouldReturnForbid()
    {
        SetUser("basic_user", "ApiUser");

        var result = await _controller.DisableEntity("1");

        Assert.IsType<ForbidResult>(result);
        _serviceMock.Verify(s => s.DisableEntityAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAllEntities_ShouldForbid_WhenUserIsNull()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // User null
        };

        var result = await _controller.GetAllEntities();
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DisableEntity_ShouldForbid_WhenUserIsNull()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // User null
        };

        var result = await _controller.DisableEntity("1");
        Assert.IsType<ForbidResult>(result);
    }
}