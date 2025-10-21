using CMCSPrototype.Models;

namespace CMCSPrototype.Services
{
    public interface IReportService
    {
        ReportViewModel GetOverallReport();
        ReportViewModel GetMonthlyReport(int year, int month);
        List<LecturerReport> GetLecturerReports();
        List<MonthlyReport> GetMonthlyReports(int year);
    }
}
