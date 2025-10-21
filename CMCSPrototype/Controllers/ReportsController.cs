using CMCSPrototype.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMCSPrototype.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var report = _reportService.GetOverallReport();
                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading reports: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult Monthly(int? year, int? month)
        {
            try
            {
                var currentYear = year ?? DateTime.Now.Year;
                var currentMonth = month ?? DateTime.Now.Month;
                
                var report = _reportService.GetMonthlyReport(currentYear, currentMonth);
                ViewBag.Year = currentYear;
                ViewBag.Month = currentMonth;
                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading monthly report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult LecturerReports()
        {
            try
            {
                var reports = _reportService.GetLecturerReports();
                return View(reports);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading lecturer reports: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
