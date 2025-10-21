using Xunit;
using Microsoft.EntityFrameworkCore;
using CMCSPrototype.Data;
using CMCSPrototype.Models;
using CMCSPrototype.Services;
using Moq;

namespace CMCSPrototype.Tests
{
    public class ReportServiceTests
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
        public void GetOverallReport_Should_Return_Correct_Statistics()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var reportService = new ReportService(context, mockLogger.Object);
            
            context.Claims.AddRange(
                new Claim { LecturerName = "User1", HoursWorked = 10, HourlyRate = 100, Status = ClaimStatus.Pending, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User2", HoursWorked = 20, HourlyRate = 100, Status = ClaimStatus.Approved, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User3", HoursWorked = 15, HourlyRate = 100, Status = ClaimStatus.Rejected, SubmissionDate = DateTime.Now }
            );
            context.SaveChanges();

            // Act
            var report = reportService.GetOverallReport();

            // Assert
            Assert.Equal(3, report.TotalClaims);
            Assert.Equal(1, report.PendingClaims);
            Assert.Equal(1, report.ApprovedClaims);
            Assert.Equal(1, report.RejectedClaims);
            Assert.Equal(2000, report.TotalApprovedAmount);
        }

        [Fact]
        public void GetLecturerReports_Should_Group_By_Lecturer()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var reportService = new ReportService(context, mockLogger.Object);
            
            context.Claims.AddRange(
                new Claim { LecturerName = "John", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Approved, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "John", HoursWorked = 5, HourlyRate = 50, Status = ClaimStatus.Pending, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "Jane", HoursWorked = 20, HourlyRate = 60, Status = ClaimStatus.Approved, SubmissionDate = DateTime.Now }
            );
            context.SaveChanges();

            // Act
            var reports = reportService.GetLecturerReports();

            // Assert
            Assert.Equal(2, reports.Count);
            
            var johnReport = reports.First(r => r.LecturerName == "John");
            Assert.Equal(2, johnReport.TotalClaims);
            Assert.Equal(1, johnReport.ApprovedClaims);
            Assert.Equal(750, johnReport.TotalAmount);
        }

        [Fact]
        public void GetMonthlyReport_Should_Filter_By_Month()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var reportService = new ReportService(context, mockLogger.Object);
            
            context.Claims.AddRange(
                new Claim { LecturerName = "User1", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Approved, SubmissionDate = new DateTime(2025, 10, 15) },
                new Claim { LecturerName = "User2", HoursWorked = 5, HourlyRate = 50, Status = ClaimStatus.Pending, SubmissionDate = new DateTime(2025, 10, 20) },
                new Claim { LecturerName = "User3", HoursWorked = 20, HourlyRate = 50, Status = ClaimStatus.Approved, SubmissionDate = new DateTime(2025, 11, 5) }
            );
            context.SaveChanges();

            // Act
            var report = reportService.GetMonthlyReport(2025, 10);

            // Assert
            Assert.Equal(2, report.TotalClaims);
            Assert.Equal(1, report.PendingClaims);
            Assert.Equal(1, report.ApprovedClaims);
        }
    }
}
