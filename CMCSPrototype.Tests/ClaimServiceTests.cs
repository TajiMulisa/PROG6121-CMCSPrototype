using Xunit;
using Microsoft.EntityFrameworkCore;
using CMCSPrototype.Data;
using CMCSPrototype.Models;
using CMCSPrototype.Services;
using Moq;

namespace CMCSPrototype.Tests
{
    public class ClaimServiceTests
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
        public async Task SubmitClaim_Should_Add_Claim_To_Database()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            var claim = new Claim
            {
                LecturerName = "John Doe",
                HoursWorked = 10,
                HourlyRate = 50,
                SubmissionDate = DateTime.Now
            };

            // Act
            await claimService.SubmitClaim(claim);

            // Assert
            var savedClaim = await context.Claims.FirstOrDefaultAsync(c => c.LecturerName == "John Doe");
            Assert.NotNull(savedClaim);
            Assert.Equal("John Doe", savedClaim.LecturerName);
            Assert.Equal(ClaimStatus.Pending, savedClaim.Status);
        }

        [Fact]
        public async Task ApproveClaim_Should_Update_Status_To_Approved()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            var claim = new Claim
            {
                Id = 1,
                LecturerName = "Jane Smith",
                HoursWorked = 5,
                HourlyRate = 60,
                Status = ClaimStatus.Pending,
                SubmissionDate = DateTime.Now
            };
            await context.Claims.AddAsync(claim);
            await context.SaveChangesAsync();

            // Act
            await claimService.ApproveClaim(1, "Manager", "Approved for payment");

            // Assert
            var updatedClaim = await context.Claims.FirstOrDefaultAsync(c => c.Id == 1);
            Assert.NotNull(updatedClaim);
            Assert.Equal(ClaimStatus.Approved, updatedClaim.Status);
            Assert.Equal("Manager", updatedClaim.ApprovedBy);
        }

        [Fact]
        public async Task RejectClaim_Should_Update_Status_To_Rejected()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            var claim = new Claim
            {
                Id = 1,
                LecturerName = "Bob Johnson",
                HoursWorked = 8,
                HourlyRate = 55,
                Status = ClaimStatus.Pending,
                SubmissionDate = DateTime.Now
            };
            await context.Claims.AddAsync(claim);
            await context.SaveChangesAsync();

            // Act
            await claimService.RejectClaim(1, "Coordinator", "Insufficient documentation");

            // Assert
            var updatedClaim = await context.Claims.FirstOrDefaultAsync(c => c.Id == 1);
            Assert.NotNull(updatedClaim);
            Assert.Equal(ClaimStatus.Rejected, updatedClaim.Status);
            Assert.Equal("Coordinator", updatedClaim.RejectedBy);
            Assert.Equal("Insufficient documentation", updatedClaim.RejectionReason);
        }

        [Fact]
        public async Task SubmitClaim_Should_Reject_Duplicate_For_Same_Month()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            
            var firstClaim = new Claim
            {
                LecturerName = "John Doe",
                HoursWorked = 10,
                HourlyRate = 50,
                SubmissionDate = new DateTime(2025, 10, 15)
            };
            await claimService.SubmitClaim(firstClaim);

            var duplicateClaim = new Claim
            {
                LecturerName = "John Doe",
                HoursWorked = 15,
                HourlyRate = 50,
                SubmissionDate = new DateTime(2025, 10, 20)
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => claimService.SubmitClaim(duplicateClaim));
        }

        [Fact]
        public async Task SubmitClaim_Should_Reject_Excessive_Hours()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            
            var claim = new Claim
            {
                LecturerName = "Jane Smith",
                HoursWorked = 250, // Exceeds 200 hour limit
                HourlyRate = 50,
                SubmissionDate = DateTime.Now
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => claimService.SubmitClaim(claim));
        }

        [Fact]
        public async Task GetPendingClaims_Should_Return_Only_Pending()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            
            await context.Claims.AddRangeAsync(
                new Claim { LecturerName = "User1", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Pending, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User2", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Approved, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User3", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Pending, SubmissionDate = DateTime.Now }
            );
            await context.SaveChangesAsync();

            // Act
            var pendingClaims = await claimService.GetPendingClaims();

            // Assert
            Assert.Equal(2, pendingClaims.Count);
            Assert.All(pendingClaims, c => Assert.Equal(ClaimStatus.Pending, c.Status));
        }
    }
}
