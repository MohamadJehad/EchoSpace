using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Services;
using EchoSpace.Infrastructure.Data;
using EchoSpace.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

