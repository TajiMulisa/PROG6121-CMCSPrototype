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
        public async Task ApproveClaim_ByCoordinator_Should_Update_Status_To_CoordinatorApproved()
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
            await claimService.ApproveClaim(1, "Coordinator", "Approved by coordinator");

            // Assert
            var updatedClaim = await context.Claims.FirstOrDefaultAsync(c => c.Id == 1);
            Assert.NotNull(updatedClaim);
            Assert.Equal(ClaimStatus.CoordinatorApproved, updatedClaim.Status);
            Assert.Equal("Coordinator", updatedClaim.CoordinatorApprovedBy);
            Assert.Equal("Approved by coordinator", updatedClaim.CoordinatorApprovalComments);
            Assert.NotNull(updatedClaim.CoordinatorApprovedAt);
        }

        [Fact]
        public async Task ApproveClaim_ByAdmin_Should_Update_Status_To_FullyApproved()
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
                Status = ClaimStatus.CoordinatorApproved,
                CoordinatorApprovedBy = "Coordinator",
                CoordinatorApprovedAt = DateTime.Now,
                SubmissionDate = DateTime.Now
            };
            await context.Claims.AddAsync(claim);
            await context.SaveChangesAsync();

            // Act
            await claimService.ApproveClaim(1, "Admin", "Final approval");

            // Assert
            var updatedClaim = await context.Claims.FirstOrDefaultAsync(c => c.Id == 1);
            Assert.NotNull(updatedClaim);
            Assert.Equal(ClaimStatus.Approved, updatedClaim.Status);
            Assert.Equal("Admin", updatedClaim.AdminApprovedBy);
            Assert.Equal("Final approval", updatedClaim.AdminApprovalComments);
            Assert.NotNull(updatedClaim.AdminApprovedAt);
        }

        [Fact]
        public async Task ApproveClaim_ByAdmin_Without_CoordinatorApproval_Should_Throw_Exception()
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

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                claimService.ApproveClaim(1, "Admin", "Trying to skip coordinator"));
        }

        [Fact]
        public async Task RejectClaim_ByCoordinator_Should_Update_Status_To_Rejected()
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
            Assert.NotNull(updatedClaim.RejectedAt);
        }

        [Fact]
        public async Task RejectClaim_ByAdmin_AfterCoordinatorApproval_Should_Update_Status_To_Rejected()
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
                Status = ClaimStatus.CoordinatorApproved,
                CoordinatorApprovedBy = "Coordinator",
                CoordinatorApprovedAt = DateTime.Now,
                SubmissionDate = DateTime.Now
            };
            await context.Claims.AddAsync(claim);
            await context.SaveChangesAsync();

            // Act
            await claimService.RejectClaim(1, "Admin", "Budget constraints");

            // Assert
            var updatedClaim = await context.Claims.FirstOrDefaultAsync(c => c.Id == 1);
            Assert.NotNull(updatedClaim);
            Assert.Equal(ClaimStatus.Rejected, updatedClaim.Status);
            Assert.Equal("Admin", updatedClaim.RejectedBy);
            Assert.Equal("Budget constraints", updatedClaim.RejectionReason);
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
        public async Task SubmitClaim_Should_Reject_Future_Submission_Date()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            
            var claim = new Claim
            {
                LecturerName = "Future User",
                HoursWorked = 10,
                HourlyRate = 50,
                SubmissionDate = DateTime.Now.AddDays(1)
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => claimService.SubmitClaim(claim));
        }

        [Fact]
        public async Task SubmitClaim_Should_Reject_Claims_Older_Than_Three_Months()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            
            var claim = new Claim
            {
                LecturerName = "Old Claim User",
                HoursWorked = 10,
                HourlyRate = 50,
                SubmissionDate = DateTime.Now.AddMonths(-4)
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => claimService.SubmitClaim(claim));
        }

        [Fact]
        public async Task GetPendingClaims_Should_Return_Pending_And_CoordinatorApproved()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            
            await context.Claims.AddRangeAsync(
                new Claim { LecturerName = "User1", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Pending, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User2", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.CoordinatorApproved, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User3", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Approved, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User4", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Pending, SubmissionDate = DateTime.Now }
            );
            await context.SaveChangesAsync();

            // Act
            var pendingClaims = await claimService.GetPendingClaims();

            // Assert
            Assert.Equal(3, pendingClaims.Count);
            Assert.Contains(pendingClaims, c => c.Status == ClaimStatus.Pending);
            Assert.Contains(pendingClaims, c => c.Status == ClaimStatus.CoordinatorApproved);
        }

        [Fact]
        public async Task GetClaimsByLecturer_Should_Return_Only_Claims_For_Specified_Lecturer()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            
            await context.Claims.AddRangeAsync(
                new Claim { LecturerName = "John Lecturer", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Pending, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "Jane Lecturer", HoursWorked = 15, HourlyRate = 60, Status = ClaimStatus.Approved, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "John Lecturer", HoursWorked = 20, HourlyRate = 50, Status = ClaimStatus.Approved, SubmissionDate = DateTime.Now }
            );
            await context.SaveChangesAsync();

            // Act
            var johnClaims = await claimService.GetClaimsByLecturer("John Lecturer");

            // Assert
            Assert.Equal(2, johnClaims.Count);
            Assert.All(johnClaims, c => Assert.Equal("John Lecturer", c.LecturerName));
        }

        [Fact]
        public async Task AddDocument_Should_Add_Document_To_Database()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            
            var claim = new Claim
            {
                Id = 1,
                LecturerName = "Test User",
                HoursWorked = 10,
                HourlyRate = 50,
                Status = ClaimStatus.Pending,
                SubmissionDate = DateTime.Now
            };
            await context.Claims.AddAsync(claim);
            await context.SaveChangesAsync();

            var document = new Document
            {
                FileName = "test.pdf",
                FilePath = "/uploads/test.pdf",
                ClaimId = 1,
                FileSize = 1024,
                ContentType = "application/pdf"
            };

            // Act
            await claimService.AddDocument(document);

            // Assert
            var savedDoc = await context.Documents.FirstOrDefaultAsync(d => d.FileName == "test.pdf");
            Assert.NotNull(savedDoc);
            Assert.Equal(1, savedDoc.ClaimId);
            Assert.Equal(1024, savedDoc.FileSize);
        }

        [Fact]
        public async Task GetDashboardStats_Should_Return_Correct_Statistics()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var claimService = new ClaimService(context, mockLogger.Object);
            
            await context.Claims.AddRangeAsync(
                new Claim { LecturerName = "User1", HoursWorked = 10, HourlyRate = 100, Status = ClaimStatus.Pending, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User2", HoursWorked = 20, HourlyRate = 100, Status = ClaimStatus.Approved, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User3", HoursWorked = 15, HourlyRate = 100, Status = ClaimStatus.Rejected, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User4", HoursWorked = 5, HourlyRate = 100, Status = ClaimStatus.CoordinatorApproved, SubmissionDate = DateTime.Now }
            );
            await context.SaveChangesAsync();

            // Act
            var stats = claimService.GetDashboardStats();

            // Assert
            Assert.Equal(4, stats.TotalClaims);
            Assert.Equal(1, stats.PendingClaims);
            Assert.Equal(1, stats.ApprovedClaims);
            Assert.Equal(1, stats.RejectedClaims);
        }
    }
}
