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
                new Claim { LecturerName = "User3", HoursWorked = 15, HourlyRate = 100, Status = ClaimStatus.Rejected, SubmissionDate = DateTime.Now },
                new Claim { LecturerName = "User4", HoursWorked = 5, HourlyRate = 100, Status = ClaimStatus.CoordinatorApproved, SubmissionDate = DateTime.Now }
            );
            context.SaveChanges();

            // Act
            var report = reportService.GetOverallReport();

            // Assert
            Assert.Equal(4, report.TotalClaims);
            Assert.Equal(1, report.PendingClaims); // Pending status
            Assert.Equal(1, report.ApprovedClaims); // Fully approved
            Assert.Equal(1, report.RejectedClaims);
            Assert.Equal(2000, report.TotalApprovedAmount); // Only fully approved claims
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
            Assert.Equal(500, johnReport.ApprovedAmount);
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
            Assert.Equal(0, report.RejectedClaims);
        }

        [Fact]
        public void GetMonthlyReport_Should_Include_CoordinatorApproved_In_Counts()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var reportService = new ReportService(context, mockLogger.Object);
            
            context.Claims.AddRange(
                new Claim { LecturerName = "User1", HoursWorked = 10, HourlyRate = 100, Status = ClaimStatus.Pending, SubmissionDate = new DateTime(2025, 10, 15) },
                new Claim { LecturerName = "User2", HoursWorked = 15, HourlyRate = 100, Status = ClaimStatus.CoordinatorApproved, SubmissionDate = new DateTime(2025, 10, 18) },
                new Claim { LecturerName = "User3", HoursWorked = 20, HourlyRate = 100, Status = ClaimStatus.Approved, SubmissionDate = new DateTime(2025, 10, 20) },
                new Claim { LecturerName = "User4", HoursWorked = 5, HourlyRate = 100, Status = ClaimStatus.Rejected, SubmissionDate = new DateTime(2025, 10, 22) }
            );
            context.SaveChanges();

            // Act
            var report = reportService.GetMonthlyReport(2025, 10);

            // Assert
            Assert.Equal(4, report.TotalClaims);
            Assert.Equal(1, report.PendingClaims);
            Assert.Equal(1, report.ApprovedClaims); // Only fully approved
            Assert.Equal(1, report.RejectedClaims);
        }

        [Fact]
        public void GetOverallReport_Should_Return_Empty_Report_When_No_Claims()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var reportService = new ReportService(context, mockLogger.Object);

            // Act
            var report = reportService.GetOverallReport();

            // Assert
            Assert.Equal(0, report.TotalClaims);
            Assert.Equal(0, report.PendingClaims);
            Assert.Equal(0, report.ApprovedClaims);
            Assert.Equal(0, report.RejectedClaims);
            Assert.Equal(0, report.TotalApprovedAmount);
            Assert.Equal(0, report.TotalPendingAmount);
            Assert.Equal(0, report.TotalRejectedAmount);
        }

        [Fact]
        public void GetLecturerReports_Should_Return_Empty_List_When_No_Claims()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var reportService = new ReportService(context, mockLogger.Object);

            // Act
            var reports = reportService.GetLecturerReports();

            // Assert
            Assert.Empty(reports);
        }

        [Fact]
        public void GetMonthlyReport_Should_Return_Empty_Report_For_Month_With_No_Claims()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockLogger = GetMockLoggingService();
            var reportService = new ReportService(context, mockLogger.Object);
            
            // Add claims for different month
            context.Claims.Add(
                new Claim { LecturerName = "User1", HoursWorked = 10, HourlyRate = 50, Status = ClaimStatus.Approved, SubmissionDate = new DateTime(2025, 11, 15) }
            );
            context.SaveChanges();

            // Act
            var report = reportService.GetMonthlyReport(2025, 10);

            // Assert
            Assert.Equal(0, report.TotalClaims);
            Assert.Equal(0, report.PendingClaims);
            Assert.Equal(0, report.ApprovedClaims);
        }
    }
}
