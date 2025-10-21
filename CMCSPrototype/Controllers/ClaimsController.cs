using CMCSPrototype.Data;
using CMCSPrototype.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCSPrototype.Models;


namespace CMCSPrototype.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly AppDbContext _context;

        public ClaimsController(IClaimService claimService, AppDbContext context)
        {
            _claimService = claimService;
            _context = context;
        }

        // Lecturer Claim Submission Page
        public IActionResult SubmitClaim()
        {
            return View();
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
            
            foreach (var document in documents)
            {
                try
                {
                    await HandleFileUpload(document, claimId);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorMessages.Add($"{document.FileName}: {ex.Message}");
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
        public IActionResult TrackStatus()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile document)
        {
            if (ModelState.IsValid)
            {
                claim.SubmissionDate = DateTime.Now;
                await _claimService.SubmitClaim(claim);
                
                if (document != null)
                {
                    try
                    {
                        await HandleFileUpload(document, claim.Id);
                        TempData["Success"] = "Claim submitted successfully with document!";
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Claim submitted but file upload failed: {ex.Message}";
                    }
                }
                else
                {
                    TempData["Success"] = "Claim submitted successfully!";
                }
                
                return RedirectToAction("TrackStatus");
            }
            return View(claim);
        }

        [HttpGet]
        public async Task<IActionResult> GetApproveClaims()
        {
            var claims = await _claimService.GetPendingClaims();
            return View("ApproveClaims", claims);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            await _claimService.ApproveClaim(id);
            return RedirectToAction("GetApproveClaims");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            await _claimService.RejectClaim(id);
            return RedirectToAction("GetApproveClaims");
        }

        private async Task HandleFileUpload(IFormFile file, int claimId)
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
            
            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            // Create document record
            var document = new Document
            {
                FileName = file.FileName,
                FilePath = filePath,
                ClaimId = claimId,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.Now
            };
            
            await _claimService.AddDocument(document);
        }

        public async Task<IActionResult> TrackStatus(string lecturerName)
        {
            var claims = await _context.Claims.Where(c => c.LecturerName == lecturerName).ToListAsync();
            return View(claims);
        }
    }
}
