using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Services;
using EchoSpace.Infrastructure.Data;
using EchoSpace.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Tests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly DbContextOptions<EchoSpaceDbContext> _options;
        private readonly EchoSpaceDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IUserService _userService;

        public UserServiceTests()
        {
            _options = new DbContextOptionsBuilder<EchoSpaceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EchoSpaceDbContext(_options);
            _userRepository = new UserRepository(_context);
            _userService = new UserService(_userRepository);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow },
                new User { Id = Guid.NewGuid(), Name = "Jane Smith", Email = "jane@example.com", CreatedAt = DateTime.UtcNow }
            };

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, u => u.Name == "John Doe");
            Assert.Contains(result, u => u.Name == "Jane Smith");
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("John Doe", result.Name);
            Assert.Equal("john@example.com", result.Email);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateUserWithGeneratedId()
        {
            // Arrange
            var request = new CreateUserRequest { Name = "John Doe", Email = "john@example.com" };

            // Act
            var result = await _userService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Email, result.Email);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);
            Assert.Null(result.UpdatedAt);

            // Verify it was actually saved to database
            var savedUser = await _context.Users.FindAsync(result.Id);
            Assert.NotNull(savedUser);
            Assert.Equal(request.Name, savedUser.Name);
        }

        [Fact]
        public async Task UpdateAsync_WithValidId_ShouldUpdateUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User { Id = userId, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
            
            await _context.Users.AddAsync(existingUser);
            await _context.SaveChangesAsync();

            var request = new UpdateUserRequest { Name = "John Updated", Email = "john.updated@example.com" };

            // Act
            var result = await _userService.UpdateAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Email, result.Email);
            Assert.NotNull(result.UpdatedAt);

            // Verify it was actually updated in database
            var updatedUser = await _context.Users.FindAsync(userId);
            Assert.NotNull(updatedUser);
            Assert.Equal(request.Name, updatedUser.Name);
            Assert.Equal(request.Email, updatedUser.Email);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new UpdateUserRequest { Name = "John Updated", Email = "john.updated@example.com" };

            // Act
            var result = await _userService.UpdateAsync(userId, request);

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
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            Assert.True(result);

            // Verify it was actually deleted from database
            var deletedUser = await _context.Users.FindAsync(userId);
            Assert.Null(deletedUser);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
