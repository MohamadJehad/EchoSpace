using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Enums;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Services;
using EchoSpace.Infrastructure.Data;
using EchoSpace.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;

namespace EchoSpace.Tests.Services
{
    public class PostServiceTests : IDisposable
    {
        private readonly DbContextOptions<EchoSpaceDbContext> _options;
        private readonly EchoSpaceDbContext _context;
        private readonly IPostRepository _postRepository;
        private readonly ILikeRepository _likeRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<PostService> _logger;
        private readonly IPostService _postService;

        public PostServiceTests()
        {
            _options = new DbContextOptionsBuilder<EchoSpaceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EchoSpaceDbContext(_options);
            _postRepository = new PostRepository(_context);
            _likeRepository = new LikeRepository(_context);
            _tagRepository = new TagRepository(_context);
            _logger = new MockLogger<PostService>();
            _postService = new PostService(_postRepository, _likeRepository, _tagRepository, null, null, null, _logger);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreatePostWithGeneratedId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "This is my first post!",
                ImageUrl = null
            };

            // Act
            var result = await _postService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.PostId);
            Assert.Equal(request.UserId, result.UserId);
            Assert.Equal(request.Content, result.Content);
            Assert.Null(result.ImageUrl);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);
            Assert.Null(result.UpdatedAt);

            // Verify it was actually saved to database
            var savedPost = await _context.Posts.FindAsync(result.PostId);
            Assert.NotNull(savedPost);
            Assert.Equal(request.Content, savedPost.Content);
            Assert.Equal(request.UserId, savedPost.UserId);
        }

        [Fact]
        public async Task CreateAsync_WithImageUrl_ShouldCreatePostWithImage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var imageUrl = "https://example.com/image.jpg";
            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Post with image",
                ImageUrl = imageUrl
            };

            // Act
            var result = await _postService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(imageUrl, result.ImageUrl);

            // Verify it was saved with image URL
            var savedPost = await _context.Posts.FindAsync(result.PostId);
            Assert.NotNull(savedPost);
            Assert.Equal(imageUrl, savedPost.ImageUrl);
        }

        [Fact]
        public async Task CreateAsync_WithTags_ShouldCreatePostWithTags()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);

            var tag1 = new Tag { TagId = Guid.NewGuid(), Name = "Technology", CreatedAt = DateTime.UtcNow };
            var tag2 = new Tag { TagId = Guid.NewGuid(), Name = "Programming", CreatedAt = DateTime.UtcNow };
            await _context.Tags.AddRangeAsync(tag1, tag2);
            await _context.SaveChangesAsync();

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Post about technology and programming",
                ImageUrl = null,
                TagIds = new List<Guid> { tag1.TagId, tag2.TagId }
            };

            // Act
            var result = await _postService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Tags.Count);
            Assert.Contains(result.Tags, t => t.TagId == tag1.TagId);
            Assert.Contains(result.Tags, t => t.TagId == tag2.TagId);

            // Verify tags were saved in database
            var savedPost = await _context.Posts
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.PostId == result.PostId);
            Assert.NotNull(savedPost);
            Assert.Equal(2, savedPost.PostTags.Count);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidTagIds_ShouldCreatePostWithoutInvalidTags()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);

            var validTag = new Tag { TagId = Guid.NewGuid(), Name = "ValidTag", CreatedAt = DateTime.UtcNow };
            await _context.Tags.AddAsync(validTag);
            await _context.SaveChangesAsync();

            var invalidTagId = Guid.NewGuid();
            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Post with mixed valid and invalid tags",
                TagIds = new List<Guid> { validTag.TagId, invalidTagId }
            };

            // Act
            var result = await _postService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            // Only valid tag should be added
            Assert.Single(result.Tags);
            Assert.Contains(result.Tags, t => t.TagId == validTag.TagId);
        }

        [Fact]
        public async Task UpdateAsync_WithValidId_ShouldUpdatePost()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);

            var postId = Guid.NewGuid();
            var existingPost = new Post
            {
                PostId = postId,
                UserId = userId,
                Content = "Original content",
                ImageUrl = null,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Posts.AddAsync(existingPost);
            await _context.SaveChangesAsync();

            var request = new UpdatePostRequest
            {
                Content = "Updated content",
                ImageUrl = "https://example.com/new-image.jpg"
            };

            // Act
            var result = await _postService.UpdateAsync(postId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Content, result.Content);
            Assert.Equal(request.ImageUrl, result.ImageUrl);
            Assert.NotNull(result.UpdatedAt);

            // Verify it was actually updated in database
            var updatedPost = await _context.Posts.FindAsync(postId);
            Assert.NotNull(updatedPost);
            Assert.Equal(request.Content, updatedPost.Content);
            Assert.Equal(request.ImageUrl, updatedPost.ImageUrl);
            Assert.NotNull(updatedPost.UpdatedAt);
        }

        [Fact]
        public async Task UpdateAsync_WithOnlyContent_ShouldUpdateContentOnly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);

            var postId = Guid.NewGuid();
            var originalImageUrl = "https://example.com/original.jpg";
            var existingPost = new Post
            {
                PostId = postId,
                UserId = userId,
                Content = "Original content",
                ImageUrl = originalImageUrl,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Posts.AddAsync(existingPost);
            await _context.SaveChangesAsync();

            var request = new UpdatePostRequest
            {
                Content = "Updated content only",
                ImageUrl = originalImageUrl // Keep same image
            };

            // Act
            var result = await _postService.UpdateAsync(postId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Content, result.Content);
            Assert.Equal(originalImageUrl, result.ImageUrl);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task UpdateAsync_WithNullImageUrl_ShouldUpdateToNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);

            var postId = Guid.NewGuid();
            var existingPost = new Post
            {
                PostId = postId,
                UserId = userId,
                Content = "Original content",
                ImageUrl = "https://example.com/image.jpg",
                CreatedAt = DateTime.UtcNow
            };
            await _context.Posts.AddAsync(existingPost);
            await _context.SaveChangesAsync();

            var request = new UpdatePostRequest
            {
                Content = "Updated content",
                ImageUrl = null
            };

            // Act
            var result = await _postService.UpdateAsync(postId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Content, result.Content);
            Assert.Null(result.ImageUrl);

            // Verify it was updated in database
            var updatedPost = await _context.Posts.FindAsync(postId);
            Assert.NotNull(updatedPost);
            Assert.Null(updatedPost.ImageUrl);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var request = new UpdatePostRequest
            {
                Content = "Updated content",
                ImageUrl = null
            };

            // Act
            var result = await _postService.UpdateAsync(postId, request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);

            var postId = Guid.NewGuid();
            var post = new Post
            {
                PostId = postId,
                UserId = userId,
                Content = "Post to be deleted",
                CreatedAt = DateTime.UtcNow
            };
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();

            // Act
            var result = await _postService.DeleteAsync(postId);

            // Assert
            Assert.True(result);

            // Verify it was actually deleted from database
            var deletedPost = await _context.Posts.FindAsync(postId);
            Assert.Null(deletedPost);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var postId = Guid.NewGuid();

            // Act
            var result = await _postService.DeleteAsync(postId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_WithPostHavingLikes_ShouldDeletePostAndCascadeLikes()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);

            var postId = Guid.NewGuid();
            var post = new Post
            {
                PostId = postId,
                UserId = userId,
                Content = "Post with likes",
                CreatedAt = DateTime.UtcNow
            };
            await _context.Posts.AddAsync(post);

            var like = new Like
            {
                LikeId = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Likes.AddAsync(like);
            await _context.SaveChangesAsync();

            // Act
            var result = await _postService.DeleteAsync(postId);

            // Assert
            Assert.True(result);

            // Verify post was deleted
            var deletedPost = await _context.Posts.FindAsync(postId);
            Assert.Null(deletedPost);

            // Verify likes were cascaded (deleted)
            var deletedLike = await _context.Likes.FindAsync(like.LikeId);
            Assert.Null(deletedLike);
        }

        [Fact]
        public async Task DeleteAsync_WithPostHavingComments_ShouldDeletePostAndCascadeComments()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);

            var postId = Guid.NewGuid();
            var post = new Post
            {
                PostId = postId,
                UserId = userId,
                Content = "Post with comments",
                CreatedAt = DateTime.UtcNow
            };
            await _context.Posts.AddAsync(post);

            var comment = new Comment
            {
                CommentId = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                Content = "A comment",
                CreatedAt = DateTime.UtcNow
            };
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _postService.DeleteAsync(postId);

            // Assert
            Assert.True(result);

            // Verify post was deleted
            var deletedPost = await _context.Posts.FindAsync(postId);
            Assert.Null(deletedPost);

            // Verify comments were cascaded (deleted)
            var deletedComment = await _context.Comments.FindAsync(comment.CommentId);
            Assert.Null(deletedComment);
        }

        #region AI Image Generation Tests

        [Fact]
        public async Task CreateAsync_WithGenerateImage_WhenServicesAreNull_ShouldCreatePostWithoutImage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Post with AI image request",
                GenerateImage = true
            };

            // PostService is initialized with null AI services in constructor
            // Act
            var result = await _postService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.ImageUrl); // Should not have image since services are null
        }

        [Fact]
        public async Task CreateAsync_WithGenerateImage_WhenAllServicesAvailable_ShouldGenerateAndStoreImage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Create a simple PNG image (minimal valid PNG bytes)
            var pngBytes = new byte[] 
            { 
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
                0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 image
                0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE,
                0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, // IDAT chunk
                0x08, 0x99, 0x01, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 // IEND
            };

            // Create test HTTP server to serve the image
            // Use a random available port
            var tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((System.Net.IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            
            var imageUrl = $"http://localhost:{port}/image.png";
            
            using var httpListener = new System.Net.HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{port}/");
            httpListener.Start();

            // Set up HTTP response handler (fire and forget - runs in background)
            var serverReady = new System.Threading.Tasks.TaskCompletionSource<bool>();
            _ = Task.Run(async () =>
            {
                try
                {
                    serverReady.SetResult(true);
                    var context = await httpListener.GetContextAsync();
                    context.Response.ContentType = "image/png";
                    context.Response.ContentLength64 = pngBytes.Length;
                    await context.Response.OutputStream.WriteAsync(pngBytes, 0, pngBytes.Length);
                    context.Response.Close();
                }
                catch
                {
                    // Ignore errors in test server
                }
            });

            // Wait for server to be ready
            await serverReady.Task;
            await Task.Delay(100); // Small delay to ensure listener is accepting connections

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Post with AI image request",
                GenerateImage = true
            };

            // Mock AI Image Generation Service
            var mockAiService = new Mock<IAiImageGenerationService>();
            mockAiService
                .Setup(x => x.GenerateImageAsync(It.IsAny<string>()))
                .ReturnsAsync(new ImageResultDto(imageUrl));

            // Mock Blob Storage Service
            var mockBlobService = new Mock<IBlobStorageService>();
            var blobUrl = "https://storage.example.com/ai-images/test-image.png";
            var sasUrl = "https://storage.example.com/ai-images/test-image.png?sas=token";
            
            mockBlobService
                .Setup(x => x.UploadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(blobUrl);

            mockBlobService
                .Setup(x => x.GetBlobUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(sasUrl);

            // Mock Image Repository
            var mockImageRepository = new Mock<IImageRepository>();
            Image? savedImage = null;
            mockImageRepository
                .Setup(x => x.AddAsync(It.IsAny<Image>()))
                .ReturnsAsync((Image img) => 
                {
                    savedImage = img;
                    return img;
                });

            // Create PostService with mocked services
            var postServiceWithAi = new PostService(
                _postRepository,
                _likeRepository,
                _tagRepository,
                mockAiService.Object,
                mockBlobService.Object,
                mockImageRepository.Object,
                _logger
            );

            // Act
            var result = await postServiceWithAi.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ImageUrl);
            Assert.Equal(sasUrl, result.ImageUrl);

            // Verify AI service was called
            mockAiService.Verify(x => x.GenerateImageAsync(request.Content), Times.Once);

            // Verify blob storage was called
            mockBlobService.Verify(x => x.UploadBlobAsync(
                "ai-images",
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                "image/png"
            ), Times.Once);

            mockBlobService.Verify(x => x.GetBlobUrlAsync(
                "ai-images",
                It.IsAny<string>(),
                60
            ), Times.Once);

            // Verify image repository was called
            mockImageRepository.Verify(x => x.AddAsync(It.Is<Image>(img =>
                img.Source == ImageSource.AIGenerated &&
                img.UserId == userId &&
                img.PostId == result.PostId &&
                img.ContainerName == "ai-images"
            )), Times.Once);

            // Verify image entity was created correctly
            Assert.NotNull(savedImage);
            Assert.Equal(ImageSource.AIGenerated, savedImage.Source);
            Assert.Equal(userId, savedImage.UserId);
            Assert.Equal(result.PostId, savedImage.PostId);

            httpListener.Stop();
        }

        [Fact]
        public async Task CreateAsync_WithGenerateImage_WhenAiServiceThrowsException_ShouldContinueWithoutImage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Post with AI image request",
                GenerateImage = true
            };

            // Mock AI Image Generation Service to throw exception
            var mockAiService = new Mock<IAiImageGenerationService>();
            mockAiService
                .Setup(x => x.GenerateImageAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("AI service unavailable"));

            // Mock Blob Storage Service
            var mockBlobService = new Mock<IBlobStorageService>();

            // Mock Image Repository
            var mockImageRepository = new Mock<IImageRepository>();

            // Create PostService with mocked services
            var postServiceWithAi = new PostService(
                _postRepository,
                _likeRepository,
                _tagRepository,
                mockAiService.Object,
                mockBlobService.Object,
                mockImageRepository.Object,
                _logger
            );

            // Act
            var result = await postServiceWithAi.CreateAsync(request);

            // Assert - Post should still be created even if AI generation fails
            Assert.NotNull(result);
            Assert.Null(result.ImageUrl); // Should not have image due to error

            // Verify AI service was called
            mockAiService.Verify(x => x.GenerateImageAsync(request.Content), Times.Once);

            // Verify blob storage was NOT called (since AI service failed)
            mockBlobService.Verify(x => x.UploadBlobAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>()
            ), Times.Never);

            // Verify image repository was NOT called
            mockImageRepository.Verify(x => x.AddAsync(It.IsAny<Image>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithGenerateImage_WhenBlobStorageFails_ShouldContinueWithoutImage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Create a simple PNG image
            var pngBytes = new byte[] 
            { 
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
                0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
                0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE,
                0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54,
                0x08, 0x99, 0x01, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
            };

            // Use a random available port
            var tcpListener2 = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            tcpListener2.Start();
            var port2 = ((System.Net.IPEndPoint)tcpListener2.LocalEndpoint).Port;
            tcpListener2.Stop();
            
            var imageUrl2 = $"http://localhost:{port2}/image.png";
            
            using var httpListener2 = new System.Net.HttpListener();
            httpListener2.Prefixes.Add($"http://localhost:{port2}/");
            httpListener2.Start();

            // Set up HTTP response handler (fire and forget - runs in background)
            var serverReady2 = new System.Threading.Tasks.TaskCompletionSource<bool>();
            _ = Task.Run(async () =>
            {
                try
                {
                    serverReady2.SetResult(true);
                    var context = await httpListener2.GetContextAsync();
                    context.Response.ContentType = "image/png";
                    context.Response.ContentLength64 = pngBytes.Length;
                    await context.Response.OutputStream.WriteAsync(pngBytes, 0, pngBytes.Length);
                    context.Response.Close();
                }
                catch
                {
                    // Ignore errors in test server
                }
            });

            // Wait for server to be ready
            await serverReady2.Task;
            await Task.Delay(100); // Small delay to ensure listener is accepting connections

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Post with AI image request",
                GenerateImage = true
            };

            // Mock AI Image Generation Service
            var mockAiService = new Mock<IAiImageGenerationService>();
            mockAiService
                .Setup(x => x.GenerateImageAsync(It.IsAny<string>()))
                .ReturnsAsync(new ImageResultDto(imageUrl2));

            // Mock Blob Storage Service to throw exception
            var mockBlobService = new Mock<IBlobStorageService>();
            mockBlobService
                .Setup(x => x.UploadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Blob storage unavailable"));

            // Mock Image Repository
            var mockImageRepository = new Mock<IImageRepository>();

            // Create PostService with mocked services
            var postServiceWithAi = new PostService(
                _postRepository,
                _likeRepository,
                _tagRepository,
                mockAiService.Object,
                mockBlobService.Object,
                mockImageRepository.Object,
                _logger
            );

            // Act
            var result = await postServiceWithAi.CreateAsync(request);

            // Assert - Post should still be created even if blob storage fails
            Assert.NotNull(result);
            Assert.Null(result.ImageUrl); // Should not have image due to error

            // Verify blob storage was called but failed
            mockBlobService.Verify(x => x.UploadBlobAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>()
            ), Times.Once);

            // Verify image repository was NOT called (since blob storage failed)
            mockImageRepository.Verify(x => x.AddAsync(It.IsAny<Image>()), Times.Never);

            httpListener2.Stop();
        }

        [Fact]
        public async Task CreateAsync_WithGenerateImageFalse_ShouldNotCallAiServices()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var request = new CreatePostRequest
            {
                UserId = userId,
                Content = "Post without AI image",
                GenerateImage = false
            };

            // Mock services
            var mockAiService = new Mock<IAiImageGenerationService>();
            var mockBlobService = new Mock<IBlobStorageService>();
            var mockImageRepository = new Mock<IImageRepository>();

            // Create PostService with mocked services
            var postServiceWithAi = new PostService(
                _postRepository,
                _likeRepository,
                _tagRepository,
                mockAiService.Object,
                mockBlobService.Object,
                mockImageRepository.Object,
                _logger
            );

            // Act
            var result = await postServiceWithAi.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.ImageUrl);

            // Verify AI service was NOT called
            mockAiService.Verify(x => x.GenerateImageAsync(It.IsAny<string>()), Times.Never);
            mockBlobService.Verify(x => x.UploadBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
            mockImageRepository.Verify(x => x.AddAsync(It.IsAny<Image>()), Times.Never);
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

