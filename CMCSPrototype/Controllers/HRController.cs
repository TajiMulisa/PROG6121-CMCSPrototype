using CMCSPrototype.Models;
using CMCSPrototype.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CMCSPrototype.Controllers
{
    public class HRController : Controller
    {
        private readonly IHRService _hrService;
        private readonly IClaimService _claimService;
        private readonly IPdfService _pdfService;

        public HRController(IHRService hrService, IClaimService claimService, IPdfService pdfService)
        {
            _hrService = hrService;
            _claimService = claimService;
            _pdfService = pdfService;
        }

        // Authorization check
        private bool IsHR()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "HR";
        }

        private IActionResult CheckHRAccess()
        {
            if (!IsHR())
            {
                TempData["Error"] = "Access denied. HR privileges required.";
                return RedirectToAction("Index", "Home");
            }
            return null!;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            var allUsers = await _hrService.GetAllUsers();
            var allClaims = await _claimService.GetAllClaims();
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var viewModel = new HRDashboardViewModel
            {
                TotalUsers = allUsers.Count,
                ActiveLecturers = allUsers.Count(u => u.Role == UserRole.Lecturer && u.IsActive),
                PendingClaims = allClaims.Count(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = allClaims.Count(c => c.Status == ClaimStatus.Approved),
                TotalPaymentsThisMonth = allClaims
                    .Where(c => c.Status == ClaimStatus.Approved && c.SubmissionDate >= startOfMonth)
                    .Sum(c => c.TotalAmount),
                RecentUsers = allUsers.OrderByDescending(u => u.CreatedAt).Take(5).ToList()
            };

            return View(viewModel);
        }

        // User Management - List all users
        public async Task<IActionResult> ManageUsers()
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            var users = await _hrService.GetAllUsers();
            return View(users);
        }

        // Create User - GET
        [HttpGet]
        public IActionResult CreateUser()
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            return View(new CreateUserViewModel());
        }

        // Create User - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            if (ModelState.IsValid)
            {
                try
                {
                    await _hrService.CreateUser(model.FullName, model.Email, model.Password, model.Role, model.HourlyRate);
                    TempData["Success"] = $"User {model.FullName} created successfully!";
                    return RedirectToAction("ManageUsers");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    TempData["Error"] = ex.Message;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the user.");
                    TempData["Error"] = $"Error: {ex.Message}";
                }
            }

            return View(model);
        }

        // Edit User - GET
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            var user = await _hrService.GetUserById(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                HourlyRate = user.HourlyRate,
                IsActive = user.IsActive
            };

            return View(viewModel);
        }

        // Edit User - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            if (ModelState.IsValid)
            {
                try
                {
                    await _hrService.UpdateUser(model.Id, model.FullName, model.Email, model.Role, model.HourlyRate, model.IsActive);
                    TempData["Success"] = $"User {model.FullName} updated successfully!";
                    return RedirectToAction("ManageUsers");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    TempData["Error"] = ex.Message;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the user.");
                    TempData["Error"] = $"Error: {ex.Message}";
                }
            }

            return View(model);
        }

        // Toggle User Status
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            try
            {
                var user = await _hrService.GetUserById(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("ManageUsers");
                }

                if (user.IsActive)
                {
                    await _hrService.DeactivateUser(id);
                    TempData["Success"] = $"User {user.FullName} deactivated successfully.";
                }
                else
                {
                    await _hrService.ActivateUser(id);
                    TempData["Success"] = $"User {user.FullName} activated successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("ManageUsers");
        }

        // Generate Reports - GET
        [HttpGet]
        public async Task<IActionResult> GenerateReports()
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            var claims = await _hrService.GetApprovedClaimsForReport();
            return View(claims);
        }

        // Generate Reports - POST (with date filters)
        [HttpPost]
        public async Task<IActionResult> GenerateReports(DateTime? startDate, DateTime? endDate)
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            var claims = await _hrService.GetApprovedClaimsForReport(startDate, endDate);
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(claims);
        }

        // Download Invoice/Report as CSV
        [HttpGet]
        public async Task<IActionResult> DownloadReport(DateTime? startDate, DateTime? endDate)
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            var claims = await _hrService.GetApprovedClaimsForReport(startDate, endDate);
            var summary = await _hrService.GetLecturerPaymentSummary(startDate, endDate);

            var csv = new StringBuilder();
            csv.AppendLine("Approved Claims Report");
            csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            if (startDate.HasValue || endDate.HasValue)
            {
                csv.AppendLine($"Period: {startDate?.ToString("yyyy-MM-dd") ?? "Start"} to {endDate?.ToString("yyyy-MM-dd") ?? "End"}");
            }
            
            csv.AppendLine();
            csv.AppendLine("Claim ID,Lecturer Name,Hours Worked,Hourly Rate,Total Amount,Submission Date,Status");

            foreach (var claim in claims)
            {
                csv.AppendLine($"{claim.Id},{claim.LecturerName},{claim.HoursWorked},{claim.HourlyRate:C},{claim.TotalAmount:C},{claim.SubmissionDate:yyyy-MM-dd},{claim.Status}");
            }

            csv.AppendLine();
            csv.AppendLine("Payment Summary by Lecturer");
            csv.AppendLine("Lecturer Name,Total Payment");

            foreach (var item in summary.OrderByDescending(x => x.Value))
            {
                csv.AppendLine($"{item.Key},{item.Value:C}");
            }

            csv.AppendLine();
            csv.AppendLine($"Grand Total,{summary.Values.Sum():C}");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"Claims_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            return File(bytes, "text/csv", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPdfReport(DateTime? startDate, DateTime? endDate)
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            var claims = await _hrService.GetApprovedClaimsForReport(startDate, endDate);
            var summary = await _hrService.GetLecturerPaymentSummary(startDate, endDate);

            var pdfBytes = _pdfService.GenerateReportPdf(claims, summary);
            var fileName = $"Claims_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }

        // Lecturer List (for quick reference)
        public async Task<IActionResult> Lecturers()
        {
            var accessCheck = CheckHRAccess();
            if (accessCheck != null) return accessCheck;

            var lecturers = await _hrService.GetLecturers();
            return View(lecturers);
        }
    }
}
