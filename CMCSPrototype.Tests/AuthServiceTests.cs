using Xunit;
using Microsoft.EntityFrameworkCore;
using CMCSPrototype.Data;
using CMCSPrototype.Models;
using CMCSPrototype.Services;
using Moq;

namespace CMCSPrototype.Tests
{
    public class AuthServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private Mock<ILoggingService> GetMockLoggingService()
        {
            return new Mock<ILoggingService>();
        }

        [Fact]
        public async Task Register_Should_Create_New_User()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var authService = new AuthService(context, mockLogger.Object);

            // Act
            var user = await authService.Register("John Doe", "john@test.com", "password123", UserRole.Lecturer);

            // Assert
            Assert.NotNull(user);
            Assert.Equal("John Doe", user.FullName);
            Assert.Equal("john@test.com", user.Email);
            Assert.Equal(UserRole.Lecturer, user.Role);
            Assert.True(user.IsActive);
        }

        [Fact]
        public async Task Register_Should_Reject_Duplicate_Email()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var authService = new AuthService(context, mockLogger.Object);
            await authService.Register("User One", "test@test.com", "password", UserRole.Lecturer);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                authService.Register("User Two", "test@test.com", "password", UserRole.Manager));
        }

        [Fact]
        public async Task Login_Should_Return_User_With_Valid_Credentials()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var authService = new AuthService(context, mockLogger.Object);
            await authService.Register("Jane Doe", "jane@test.com", "password123", UserRole.Coordinator);

            // Act
            var user = await authService.Login("jane@test.com", "password123");

            // Assert
            Assert.NotNull(user);
            Assert.Equal("Jane Doe", user.FullName);
        }

        [Fact]
        public async Task Login_Should_Return_Null_With_Invalid_Password()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var authService = new AuthService(context, mockLogger.Object);
            await authService.Register("Test User", "test@test.com", "correctpassword", UserRole.Lecturer);

            // Act
            var user = await authService.Login("test@test.com", "wrongpassword");

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task Login_Should_Return_Null_With_Nonexistent_Email()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var authService = new AuthService(context, mockLogger.Object);

            // Act
            var user = await authService.Login("nonexistent@test.com", "password");

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public void HashPassword_Should_Generate_Consistent_Hash()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var authService = new AuthService(context, mockLogger.Object);
            var password = "testpassword123";

            // Act
            var hash1 = authService.HashPassword(password);
            var hash2 = authService.HashPassword(password);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void VerifyPassword_Should_Return_True_For_Correct_Password()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var authService = new AuthService(context, mockLogger.Object);
            var password = "mypassword";
            var hash = authService.HashPassword(password);

            // Act
            var result = authService.VerifyPassword(password, hash);

            // Assert
            Assert.True(result);
        }
    }
}
