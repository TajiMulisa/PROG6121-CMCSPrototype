using CMCSPrototype.Data;
using CMCSPrototype.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCSPrototype.Models;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace CMCSPrototype.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly AppDbContext _context;
        private readonly IEncryptionService _encryptionService;

        public ClaimsController(IClaimService claimService, AppDbContext context, IEncryptionService encryptionService)
        {
            _claimService = claimService;
            _context = context;
            _encryptionService = encryptionService;
        }

        // Lecturer Claim Submission Page
        public async Task<IActionResult> SubmitClaim()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var userIdStr = HttpContext.Session.GetString("UserId");
            
            if (string.IsNullOrEmpty(userRole) || string.IsNullOrEmpty(userIdStr))
            {
                TempData["Warning"] = "Please log in to continue.";
                return RedirectToAction("Login", "Account");
            }
            
            if (userRole != "Lecturer")
            {
                TempData["Error"] = "Only Lecturers can submit claims.";
                return RedirectToAction("Index", "Home");
            }

            // Get user's hourly rate from database
            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null || !user.HourlyRate.HasValue)
            {
                TempData["Error"] = "Your hourly rate has not been set. Please contact HR.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new SubmitClaimViewModel
            {
                HourlyRate = user.HourlyRate.Value,
                Months = Enumerable.Range(1, 12).Select(i => new SelectListItem
                {
                    Value = i.ToString(),
                    Text = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
                }),
                Years = Enumerable.Range(0, 5).Select(i => new SelectListItem
                {
                    Value = (DateTime.Now.Year - i).ToString(),
                    Text = (DateTime.Now.Year - i).ToString()
                })
            };
            
            return View(viewModel);
        }

        // Programme Coordinator / Academic Manager Claim Approval Page
        public IActionResult ApproveClaims()
        {
            return View();
        }

        // Document Upload Page
        public IActionResult UploadDocuments()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> UploadDocuments(int claimId, List<IFormFile> documents)
        {
            if (documents == null || !documents.Any())
            {
                TempData["Error"] = "Please select at least one document to upload.";
                return RedirectToAction("UploadDocuments");
            }
            
            var successCount = 0;
            var errorMessages = new List<string>();
            
            foreach (var documentFile in documents)
            {
                try
                {
                    var document = await HandleFileUpload(documentFile);
                    document.ClaimId = claimId; // Set the claimId after HandleFileUpload returns the document
                    await _claimService.AddDocument(document);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorMessages.Add($"{documentFile.FileName}: {ex.Message}");
                }
            }
            
            if (successCount > 0)
            {
                TempData["Success"] = $"{successCount} document(s) uploaded successfully.";
            }
            
            if (errorMessages.Any())
            {
                TempData["Error"] = string.Join("; ", errorMessages);
            }
            
            return RedirectToAction("TrackStatus");
        }

        // Claim Status Tracking Page
        public async Task<IActionResult> TrackStatus()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userRole))
            {
                // If no user is logged in, redirect to role selection
                TempData["Warning"] = "Please select a role to continue.";
                return RedirectToAction("SelectRole", "Account");
            }

            // Get claims based on role
            var claims = new List<Claim>();
            
            if (userRole == "Lecturer")
            {
                // Lecturers see only their own claims
                claims = await _context.Claims
                    .Include(c => c.Documents)
                    .Where(c => c.LecturerName == userName)
                    .OrderByDescending(c => c.SubmissionDate)
                    .ToListAsync();
            }
            else if (userRole == "Coordinator" || userRole == "Manager")
            {
                // Coordinators and Admins see all claims
                claims = await _context.Claims
                    .Include(c => c.Documents)
                    .OrderByDescending(c => c.SubmissionDate)
                    .ToListAsync();
            }
                
            return View(claims);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitClaim(SubmitClaimViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userName = HttpContext.Session.GetString("UserName");
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    var role = HttpContext.Session.GetString("UserRole");

                    if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userIdStr) || role != "Lecturer")
                    {
                        TempData["Error"] = "Invalid session. Please log in again.";
                        return RedirectToAction("Login", "Account");
                    }

                    var userId = int.Parse(userIdStr);
                    var user = await _context.Users.FindAsync(userId);

                    if (user == null || !user.HourlyRate.HasValue)
                    {
                        TempData["Error"] = "Your hourly rate has not been set. Please contact HR.";
                        return RedirectToAction("Index", "Home");
                    }

                    var claim = new Claim
                    {
                        LecturerName = userName,
                        HoursWorked = model.HoursWorked,
                        HourlyRate = user.HourlyRate.Value,
                        Notes = model.Notes,
                        SubmissionDate = new DateTime(model.ClaimYear, model.ClaimMonth, 1),
                        ClaimMonth = model.ClaimMonth,
                        ClaimYear = model.ClaimYear,
                        SubmittedAt = DateTime.Now,
                        Status = ClaimStatus.Pending
                    };

                    if (model.Document != null && model.Document.Length > 0)
                    {
                        var document = await HandleFileUpload(model.Document);
                        claim.Documents.Add(document);
                    }

                    await _claimService.SubmitClaim(claim);

                    TempData["Success"] = "Claim submitted successfully!";
                    return RedirectToAction("TrackStatus");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    TempData["Error"] = $"Submission failed: {ex.Message}";
                }
            }

            model.Months = Enumerable.Range(1, 12).Select(i => new SelectListItem
            {
                Value = i.ToString(),
                Text = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
            });
            model.Years = Enumerable.Range(0, 5).Select(i => new SelectListItem
            {
                Value = (DateTime.Now.Year - i).ToString(),
                Text = (DateTime.Now.Year - i).ToString()
            });

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetApproveClaims()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
            {
                TempData["Warning"] = "Please log in to continue.";
                return RedirectToAction("Login", "Account");
            }

            var claims = await _claimService.GetPendingClaims(role);
            return View("ApproveClaims", claims);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id, string approverName, string comments)
        {
            try
            {
                var role = HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrWhiteSpace(role) || (role != "Coordinator" && role != "Manager"))
                {
                    TempData["Error"] = "Only Coordinator or Manager can approve claims.";
                    return RedirectToAction("GetApproveClaims");
                }
                await _claimService.ApproveClaim(id, role, comments ?? "Approved");
                TempData["Success"] = $"Claim approved by {role}!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error approving claim: {ex.Message}";
            }
            return RedirectToAction("GetApproveClaims");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string rejectorName, string reason)
        {
            try
            {
                var role = HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrWhiteSpace(role) || (role != "Coordinator" && role != "Manager"))
                {
                    TempData["Error"] = "Only Coordinator or Manager can reject claims.";
                    return RedirectToAction("GetApproveClaims");
                }
                
                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["Error"] = "Rejection reason is required.";
                    return RedirectToAction("GetApproveClaims");
                }
                
                // Use the role as the rejector name for consistency
                await _claimService.RejectClaim(id, role, reason);
                TempData["Success"] = "Claim rejected successfully!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error rejecting claim: {ex.Message}";
            }
            
            return RedirectToAction("GetApproveClaims");
        }
        
        [HttpGet]
        public async Task<IActionResult> ClaimDetails(int id)
        {
            try
            {
                var claim = await _claimService.GetClaimById(id);
                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction("GetApproveClaims");
                }
                return View(claim);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading claim details: {ex.Message}";
                return RedirectToAction("GetApproveClaims");
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> ClaimHistory(int id)
        {
            try
            {
                var history = await _claimService.GetClaimHistory(id);
                ViewBag.ClaimId = id;
                return View(history);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading claim history: {ex.Message}";
                return RedirectToAction("TrackStatus");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _context.Documents.FindAsync(id);

            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction("TrackStatus");
            }

            var encryptedFilePath = document.FilePath;
            if (!System.IO.File.Exists(encryptedFilePath))
            {
                TempData["Error"] = "File not found on server.";
                return RedirectToAction("TrackStatus");
            }

            var encryptedData = await System.IO.File.ReadAllBytesAsync(encryptedFilePath);
            var decryptedStream = _encryptionService.Decrypt(encryptedData);

            return File(decryptedStream, document.ContentType, document.FileName);
        }

        private async Task<Document> HandleFileUpload(IFormFile file)
        {
            // Validate file size (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                throw new Exception("File size exceeds 5MB limit.");
            }
            
            // Validate file type
            var allowedTypes = new[] 
            { 
                "application/pdf", 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
                "application/msword", // .doc
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
                "application/vnd.ms-excel", // .xls
                "image/jpeg",
                "image/png"
            };
            
            if (!allowedTypes.Contains(file.ContentType))
            {
                throw new Exception("Invalid file type. Allowed types: PDF, Word, Excel, JPEG, PNG");
            }
            
            // Validate file extension
            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new Exception("Invalid file extension.");
            }
            
            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine("wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}.enc";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = file.OpenReadStream())
            {
                var encryptedData = _encryptionService.Encrypt(fileStream);
                await System.IO.File.WriteAllBytesAsync(filePath, encryptedData);
            }
            
            // Create document record
            return new Document
            {
                FileName = file.FileName,
                FilePath = filePath,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.Now
            };
        }
    }
}
