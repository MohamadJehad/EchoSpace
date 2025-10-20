using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Services;
using Moq;

namespace EchoSpace.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockRepository = new Mock<IUserRepository>();
            _userService = new UserService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "John Doe", Email = "john@example.com" },
                new User { Id = Guid.NewGuid(), Name = "Jane Smith", Email = "jane@example.com" }
            };
            _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _userService.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
            _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("John Doe", result.Name);
            _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateUserWithGeneratedId()
        {
            // Arrange
            var request = new CreateUserRequest { Name = "John Doe", Email = "john@example.com" };
            var createdUser = new User { Id = Guid.NewGuid(), Name = request.Name, Email = request.Email };
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(createdUser);

            // Act
            var result = await _userService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Email, result.Email);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);
            Assert.Null(result.UpdatedAt);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithValidId_ShouldUpdateUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
            var request = new UpdateUserRequest { Name = "John Updated", Email = "john.updated@example.com" };
            
            _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(existingUser);

            // Act
            var result = await _userService.UpdateAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Email, result.Email);
            Assert.NotNull(result.UpdatedAt);
            _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new UpdateUserRequest { Name = "John Updated", Email = "john.updated@example.com" };
            _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.UpdateAsync(userId, request);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockRepository.Setup(r => r.DeleteAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockRepository.Setup(r => r.DeleteAsync(userId)).ReturnsAsync(false);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.DeleteAsync(userId), Times.Once);
        }
    }
}
