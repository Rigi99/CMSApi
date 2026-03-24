using CMSApi.Controllers;
using CMSApi.Dtos;
using CMSApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CMSApi.Tests;

public class CmsEntityControllerTests
{
    private readonly Mock<ICmsEntityService> _serviceMock;
    private readonly Mock<ILogger<CmsEntityController>> _loggerMock;
    private readonly CmsEntityController _controller;

    public CmsEntityControllerTests()
    {
        _serviceMock = new Mock<ICmsEntityService>();
        _loggerMock = new Mock<ILogger<CmsEntityController>>();

        _controller = new CmsEntityController(
            _serviceMock.Object,
            _loggerMock.Object
        );
    }

    private void SetUser(string username, string role)
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
        ], "Basic");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task PostEvents_AsAuthorizedUser_ShouldReturnOk()
    {
        SetUser("cms_ingest_user", "CmsIngest");
        var events = new List<CmsEntityDto>
        {
            new() { Id = "1", Type = "publish", Version = 1, Timestamp = DateTime.UtcNow }
        };

        var result = await _controller.PostEvents(events);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.Contains("Processed 1 events", okResult.Value!.ToString());
        _serviceMock.Verify(s => s.ProcessEventsAsync(It.IsAny<IEnumerable<CmsEntityDto>>()), Times.Once);
    }

    [Fact]
    public async Task PostEvents_AsUnauthorizedUser_ShouldReturnForbid()
    {
        SetUser("other_user", "ApiUser");
        var events = new List<CmsEntityDto>
        {
            new() { Id = "1", Type = "publish", Version = 1, Timestamp = DateTime.UtcNow }
        };

        var result = await _controller.PostEvents(events);

        Assert.IsType<ForbidResult>(result);
        _serviceMock.Verify(s => s.ProcessEventsAsync(It.IsAny<IEnumerable<CmsEntityDto>>()), Times.Never);
    }

    [Fact]
    public async Task PostEvents_EmptyList_ShouldReturnBadRequest()
    {
        SetUser("cms_ingest_user", "CmsIngest");

        var result = await _controller.PostEvents([]);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        Assert.Contains("No events received", badRequest.Value!.ToString());
        _serviceMock.Verify(s => s.ProcessEventsAsync(It.IsAny<IEnumerable<CmsEntityDto>>()), Times.Never);
    }

    [Fact]
    public async Task PostEvents_InvalidOperationException_ShouldReturnBadRequest()
    {
        SetUser("cms_ingest_user", "CmsIngest");
        var events = new List<CmsEntityDto>
        {
            new() { Id = "1", Type = "publish", Version = 1, Timestamp = DateTime.UtcNow }
        };

        _serviceMock.Setup(s => s.ProcessEventsAsync(events))
                    .ThrowsAsync(new InvalidOperationException("Invalid event"));

        var result = await _controller.PostEvents(events);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        Assert.Contains("Invalid event", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task PostEvents_Exception_ShouldReturnInternalServerError()
    {
        SetUser("cms_ingest_user", "CmsIngest");
        var events = new List<CmsEntityDto>
        {
            new() { Id = "1", Type = "publish", Version = 1, Timestamp = DateTime.UtcNow }
        };

        _serviceMock.Setup(s => s.ProcessEventsAsync(events))
                    .ThrowsAsync(new Exception("Something went wrong"));

        var result = await _controller.PostEvents(events);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.NotNull(statusResult.Value);
        Assert.Contains("Internal server error", statusResult.Value!.ToString());
    }

    [Fact]
    public async Task PostEvents_ShouldReturnBadRequest_WhenEventsNull()
    {
        SetUser("cms_ingest_user", "CmsIngest");
        var result = await _controller.PostEvents(null!);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("No events received", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task PostEvents_ShouldReturnBadRequest_WhenEventsEmpty()
    {
        SetUser("cms_ingest_user", "CmsIngest");
        var result = await _controller.PostEvents(Array.Empty<CmsEntityDto>());
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("No events received", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task PostEvents_ShouldReturnForbid_WhenUserUnauthorized()
    {
        SetUser("nonadmin_user", "ApiUser");
        var events = new[] { new CmsEntityDto { Id = "1", Type = "publish", Version = 1, Timestamp = DateTime.UtcNow } };
        var result = await _controller.PostEvents(events);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PostEvents_ShouldReturnInternalServerError_OnException()
    {
        SetUser("cms_ingest_user", "CmsIngest");
        var events = new[] { new CmsEntityDto { Id = "1", Type = "publish", Version = 1, Timestamp = DateTime.UtcNow } };
        _serviceMock.Setup(s => s.ProcessEventsAsync(events)).ThrowsAsync(new Exception("DB failure"));

        var result = await _controller.PostEvents(events);
        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}