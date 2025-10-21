using CMCSPrototype.Data;
using CMCSPrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCSPrototype.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;
        private readonly ILoggingService _loggingService;

        public ReportService(AppDbContext context, ILoggingService loggingService)
        {
            _context = context;
            _loggingService = loggingService;
        }

        public ReportViewModel GetOverallReport()
        {
            var claims = _context.Claims.ToList();
            
            var report = new ReportViewModel
            {
                TotalClaims = claims.Count,
                PendingClaims = claims.Count(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = claims.Count(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = claims.Count(c => c.Status == ClaimStatus.Rejected),
                TotalApprovedAmount = claims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount),
                TotalPendingAmount = claims.Where(c => c.Status == ClaimStatus.Pending).Sum(c => c.TotalAmount),
                TotalRejectedAmount = claims.Where(c => c.Status == ClaimStatus.Rejected).Sum(c => c.TotalAmount),
                MonthlyReports = GetMonthlyReports(DateTime.Now.Year),
                LecturerReports = GetLecturerReports()
            };

            _loggingService.LogInfo("Overall report generated");
            return report;
        }

        public ReportViewModel GetMonthlyReport(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            var claims = _context.Claims
                .Where(c => c.SubmissionDate >= startDate && c.SubmissionDate <= endDate)
                .ToList();

            var report = new ReportViewModel
            {
                TotalClaims = claims.Count,
                PendingClaims = claims.Count(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = claims.Count(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = claims.Count(c => c.Status == ClaimStatus.Rejected),
                TotalApprovedAmount = claims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount),
                TotalPendingAmount = claims.Where(c => c.Status == ClaimStatus.Pending).Sum(c => c.TotalAmount),
                TotalRejectedAmount = claims.Where(c => c.Status == ClaimStatus.Rejected).Sum(c => c.TotalAmount)
            };

            _loggingService.LogInfo($"Monthly report generated for {year}/{month}");
            return report;
        }

        public List<LecturerReport> GetLecturerReports()
        {
            var lecturerReports = _context.Claims
                .GroupBy(c => c.LecturerName)
                .Select(g => new LecturerReport
                {
                    LecturerName = g.Key,
                    TotalClaims = g.Count(),
                    ApprovedClaims = g.Count(c => c.Status == ClaimStatus.Approved),
                    RejectedClaims = g.Count(c => c.Status == ClaimStatus.Rejected),
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    ApprovedAmount = g.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount)
                })
                .OrderByDescending(r => r.TotalAmount)
                .ToList();

            return lecturerReports;
        }

        public List<MonthlyReport> GetMonthlyReports(int year)
        {
            var monthlyReports = new List<MonthlyReport>();

            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var claims = _context.Claims
                    .Where(c => c.SubmissionDate >= startDate && c.SubmissionDate <= endDate)
                    .ToList();

                if (claims.Any())
                {
                    monthlyReports.Add(new MonthlyReport
                    {
                        Month = startDate.ToString("MMMM yyyy"),
                        ClaimsCount = claims.Count,
                        TotalAmount = claims.Sum(c => c.TotalAmount),
                        ApprovedCount = claims.Count(c => c.Status == ClaimStatus.Approved),
                        RejectedCount = claims.Count(c => c.Status == ClaimStatus.Rejected)
                    });
                }
            }

            return monthlyReports;
        }
    }
}
