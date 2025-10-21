using Xunit;
using Microsoft.EntityFrameworkCore;
using CMCSPrototype.Data;
using CMCSPrototype.Models;
using CMCSPrototype.Services;

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

        [Fact]
        public async Task SubmitClaim_Should_Add_Claim_To_Database()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var claimService = new ClaimService(context);
            var claim = new Claim
            {
                Id = 1,
                LecturerName = "Tasha Mulisa",
                HoursWorked = 10,
                HourlyRate = 50,
                Status = ClaimStatus.Pending,
                SubmissionDate = DateTime.Now
            };

            // Act
            await claimService.SubmitClaim(claim);

            // Assert
            var savedClaim = await context.Claims.FirstOrDefaultAsync(c => c.Id == 1);
            Assert.NotNull(savedClaim);
            Assert.Equal("John Doe", savedClaim.LecturerName);
        }

        [Fact]
        public async Task ApproveClaim_Should_Update_Status_To_Approved()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var claimService = new ClaimService(context);
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
            await claimService.ApproveClaim(1);

            // Assert
            var updatedClaim = await context.Claims.FirstOrDefaultAsync(c => c.Id == 1);
            Assert.NotNull(updatedClaim);
            Assert.Equal(ClaimStatus.Approved, updatedClaim.Status);
        }

        [Fact]
        public async Task RejectClaim_Should_Update_Status_To_Rejected()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var claimService = new ClaimService(context);
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
            await claimService.RejectClaim(1);

            // Assert
            var updatedClaim = await context.Claims.FirstOrDefaultAsync(c => c.Id == 1);
            Assert.NotNull(updatedClaim);
            Assert.Equal(ClaimStatus.Rejected, updatedClaim.Status);
        }
    }
}
