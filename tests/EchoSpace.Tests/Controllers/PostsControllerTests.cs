using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Interfaces;
using EchoSpace.UI.Controllers;
using Xunit;

namespace EchoSpace.Tests.Controllers
{
    public class PostsControllerTests
    {
        private readonly Mock<ILogger<PostsController>> _mockLogger;
        private readonly Mock<IPostService> _mockPostService;
        private readonly Mock<ILikeService> _mockLikeService;
        private readonly Mock<IAuditLogDBService> _mockAuditLogDBService;
        private readonly Mock<IPostReportService> _mockPostReportService;
        private readonly Mock<IFollowRepository> _mockFollowRepository;
        private readonly PostsController _controller;

        public PostsControllerTests()
        {
            _mockLogger = new Mock<ILogger<PostsController>>();
            _mockPostService = new Mock<IPostService>();
            _mockLikeService = new Mock<ILikeService>();
            _mockAuditLogDBService = new Mock<IAuditLogDBService>();
            _mockPostReportService = new Mock<IPostReportService>();
            _mockFollowRepository = new Mock<IFollowRepository>();

            _controller = new PostsController(
                _mockLogger.Object,
                _mockPostService.Object,
                _mockLikeService.Object,
                _mockAuditLogDBService.Object,
                _mockPostReportService.Object,
                _mockFollowRepository.Object
            );
        }

        private void SetupUserContext(Guid userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal,
                    Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
                }
            };
        }

        #region CreatePost Tests

        [Fact]
        public async Task CreatePost_WithValidRequest_ShouldReturnCreatedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId);

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Test post content",
                ImageUrl = null
            };

            var createdPost = new PostDto
            {
                PostId = Guid.NewGuid(),
                UserId = userId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _mockPostService
                .Setup(x => x.CreateAsync(It.IsAny<CreatePostRequest>()))
                .ReturnsAsync(createdPost);

            // Act
            var result = await _controller.CreatePost(request, CancellationToken.None);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedPost = Assert.IsType<PostDto>(createdAtActionResult.Value);
            Assert.Equal(createdPost.PostId, returnedPost.PostId);
            Assert.Equal(request.Content, returnedPost.Content);

            _mockPostService.Verify(x => x.CreateAsync(It.Is<CreatePostRequest>(r => r.UserId == userId && r.Content == request.Content)), Times.Once);
            _mockAuditLogDBService.Verify(x => x.LogAsync(
                "PostCreated",
                It.Is<Guid?>(id => id == userId),
                It.IsAny<string>(),
                It.IsAny<object>(),
                createdPost.PostId.ToString(),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task CreatePost_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId);

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = string.Empty, // Invalid - required field
                ImageUrl = null
            };

            _controller.ModelState.AddModelError("Content", "Content is required");

            // Act
            var result = await _controller.CreatePost(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            _mockPostService.Verify(x => x.CreateAsync(It.IsAny<CreatePostRequest>()), Times.Never);
        }

        [Fact]
        public async Task CreatePost_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId);

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Test post content",
                ImageUrl = null
            };

            _mockPostService
                .Setup(x => x.CreateAsync(It.IsAny<CreatePostRequest>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CreatePost(request, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            _mockAuditLogDBService.Verify(x => x.LogAsync(
                "PostCreateFailed",
                It.Is<Guid?>(id => id == userId),
                It.IsAny<string>(),
                It.IsAny<object>(),
                null,
                It.IsAny<string>()
            ), Times.Once);
        }

        #endregion

        #region GetPost Tests

        [Fact]
        public async Task GetPost_WithValidId_ShouldReturnOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            var post = new PostDto
            {
                PostId = postId,
                UserId = userId,
                Content = "Test post",
                CreatedAt = DateTime.UtcNow
            };

            _mockPostService
                .Setup(x => x.GetByIdAsync(postId, userId))
                .ReturnsAsync(post);

            _mockLikeService
                .Setup(x => x.IsLikedByUserAsync(postId, userId))
                .ReturnsAsync(false);

            _mockLikeService
                .Setup(x => x.GetLikeCountAsync(postId))
                .ReturnsAsync(0);

            // Act
            var result = await _controller.GetPost(postId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPost = Assert.IsType<PostDto>(okResult.Value);
            Assert.Equal(postId, returnedPost.PostId);
            Assert.Equal("Test post", returnedPost.Content);

            _mockPostService.Verify(x => x.GetByIdAsync(postId, userId), Times.Once);
            _mockLikeService.Verify(x => x.IsLikedByUserAsync(postId, userId), Times.Once);
            _mockLikeService.Verify(x => x.GetLikeCountAsync(postId), Times.Once);
        }

        [Fact]
        public async Task GetPost_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            _mockPostService
                .Setup(x => x.GetByIdAsync(postId, userId))
                .ReturnsAsync((PostDto?)null);

            // Act
            var result = await _controller.GetPost(postId, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains(postId.ToString(), notFoundResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task GetPost_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            _mockPostService
                .Setup(x => x.GetByIdAsync(postId, userId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetPost(postId, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetPosts Tests

        [Fact]
        public async Task GetPosts_ShouldReturnOkResultWithPosts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId);

            var posts = new List<PostDto>
            {
                new PostDto { PostId = Guid.NewGuid(), UserId = userId, Content = "Post 1", CreatedAt = DateTime.UtcNow },
                new PostDto { PostId = Guid.NewGuid(), UserId = userId, Content = "Post 2", CreatedAt = DateTime.UtcNow }
            };

            _mockPostService
                .Setup(x => x.GetAllAsync(userId))
                .ReturnsAsync(posts);

            _mockLikeService
                .Setup(x => x.IsLikedByUserAsync(It.IsAny<Guid>(), userId))
                .ReturnsAsync(false);

            _mockLikeService
                .Setup(x => x.GetLikeCountAsync(It.IsAny<Guid>()))
                .ReturnsAsync(0);

            // Act
            var result = await _controller.GetPosts(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPosts = Assert.IsAssignableFrom<IEnumerable<PostDto>>(okResult.Value);
            Assert.Equal(2, returnedPosts.Count());

            _mockPostService.Verify(x => x.GetAllAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetPosts_WhenNoPostsExist_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId);

            _mockPostService
                .Setup(x => x.GetAllAsync(userId))
                .ReturnsAsync(new List<PostDto>());

            // Act
            var result = await _controller.GetPosts(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPosts = Assert.IsAssignableFrom<IEnumerable<PostDto>>(okResult.Value);
            Assert.Empty(returnedPosts);
        }

        [Fact]
        public async Task GetPosts_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserContext(userId);

            _mockPostService
                .Setup(x => x.GetAllAsync(userId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetPosts(CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region UpdatePost Tests

        [Fact]
        public async Task UpdatePost_WithValidRequest_ShouldReturnOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            var request = new UpdatePostRequest
            {
                Content = "Updated content",
                ImageUrl = "https://example.com/image.jpg"
            };

            var updatedPost = new PostDto
            {
                PostId = postId,
                UserId = userId,
                Content = request.Content,
                ImageUrl = request.ImageUrl,
                UpdatedAt = DateTime.UtcNow
            };

            _mockPostService
                .Setup(x => x.UpdateAsync(postId, request))
                .ReturnsAsync(updatedPost);

            // Act
            var result = await _controller.UpdatePost(postId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPost = Assert.IsType<PostDto>(okResult.Value);
            Assert.Equal(request.Content, returnedPost.Content);
            Assert.Equal(request.ImageUrl, returnedPost.ImageUrl);

            _mockPostService.Verify(x => x.UpdateAsync(postId, request), Times.Once);
            _mockAuditLogDBService.Verify(x => x.LogAsync(
                "PostUpdated",
                It.Is<Guid?>(id => id == userId),
                It.IsAny<string>(),
                It.IsAny<object>(),
                postId.ToString(),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task UpdatePost_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            var request = new UpdatePostRequest
            {
                Content = string.Empty, // Invalid
                ImageUrl = null
            };

            _controller.ModelState.AddModelError("Content", "Content is required");

            // Act
            var result = await _controller.UpdatePost(postId, request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            _mockPostService.Verify(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdatePostRequest>()), Times.Never);
        }

        [Fact]
        public async Task UpdatePost_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            var request = new UpdatePostRequest
            {
                Content = "Updated content",
                ImageUrl = null
            };

            _mockPostService
                .Setup(x => x.UpdateAsync(postId, request))
                .ReturnsAsync((PostDto?)null);

            // Act
            var result = await _controller.UpdatePost(postId, request, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains(postId.ToString(), notFoundResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task UpdatePost_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            var request = new UpdatePostRequest
            {
                Content = "Updated content",
                ImageUrl = null
            };

            _mockPostService
                .Setup(x => x.UpdateAsync(postId, request))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.UpdatePost(postId, request, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            _mockAuditLogDBService.Verify(x => x.LogAsync(
                "PostUpdateFailed",
                It.Is<Guid?>(id => id == userId),
                It.IsAny<string>(),
                It.IsAny<object>(),
                null,
                It.IsAny<string>()
            ), Times.Once);
        }

        #endregion

        #region DeletePost Tests

        [Fact]
        public async Task DeletePost_WithValidId_ShouldReturnNoContent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            _mockPostService
                .Setup(x => x.DeleteAsync(postId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePost(postId, CancellationToken.None);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockPostService.Verify(x => x.DeleteAsync(postId), Times.Once);
            _mockAuditLogDBService.Verify(x => x.LogAsync(
                "PostDeleted",
                It.Is<Guid?>(id => id == userId),
                It.IsAny<string>(),
                null,
                postId.ToString(),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task DeletePost_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            _mockPostService
                .Setup(x => x.DeleteAsync(postId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePost(postId, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains(postId.ToString(), notFoundResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task DeletePost_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            SetupUserContext(userId);

            _mockPostService
                .Setup(x => x.DeleteAsync(postId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.DeletePost(postId, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            _mockAuditLogDBService.Verify(x => x.LogAsync(
                "PostUpdateFailed",
                It.Is<Guid?>(id => id == userId),
                It.IsAny<string>(),
                It.IsAny<object>(),
                null,
                It.IsAny<string>()
            ), Times.Once);
        }

        #endregion
    }
}

