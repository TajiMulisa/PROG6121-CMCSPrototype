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
                await _claimService.SubmitClaim(claim);
                if (document != null) await HandleFileUpload(document, claim.Id);
                TempData["Message"] = "Claim submitted successfully!";
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
            if (file.Length > 5 * 1024 * 1024) throw new Exception("File too large.");
            var allowedTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
            if (!allowedTypes.Contains(file.ContentType)) throw new Exception("Invalid file type.");
            var path = Path.Combine("wwwroot/uploads", Guid.NewGuid() + Path.GetExtension(file.FileName));
            using (var stream = new FileStream(path, FileMode.Create)) { await file.CopyToAsync(stream); }
            var doc = new Document { FileName = file.FileName, FilePath = path, ClaimId = claimId };
            await _claimService.AddDocument(doc);
        }

        public async Task<IActionResult> TrackStatus(string lecturerName)
        {
            var claims = await _context.Claims.Where(c => c.LecturerName == lecturerName).ToListAsync();
            return View(claims);
        }
    }
}
