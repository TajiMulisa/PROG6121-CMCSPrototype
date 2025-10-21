using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CMCSPrototype.Data;  
using CMCSPrototype.Models;  

namespace CMCSPrototype.Services
{
    public class ClaimService : IClaimService
    {
        private readonly AppDbContext _context;
        private readonly ILoggingService _loggingService;
        
        public ClaimService(AppDbContext context, ILoggingService loggingService)
        {
            _context = context;
            _loggingService = loggingService;
        }
        // Get all pending claims for approval
        public async Task<List<Claim>> GetPendingClaims()
        {
            return await _context.Claims.Where(c => c.Status == ClaimStatus.Pending).ToListAsync();
        }
        // Get a specific claim by ID (including documents)
        public async Task<Claim?> GetClaimById(int id)
        {
            return await _context.Claims.Include(c => c.Documents).FirstOrDefaultAsync(c => c.Id == id);
        }
        
        // Submit a new claim with automated verification
        public async Task SubmitClaim(Claim claim)
        {
            // Perform automated verification before submitting
            await ValidateClaim(claim);
            
            claim.SubmittedAt = DateTime.Now;
            claim.Status = ClaimStatus.Pending;
            
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
            
            // Record history
            await AddClaimHistory(claim.Id, ClaimStatus.Pending, claim.LecturerName, "Claim submitted", "Submitted");
            
            _loggingService.LogInfo($"Claim submitted by {claim.LecturerName} for R{claim.TotalAmount:N2}");
        }
        
        // Automated claim verification logic
        private async Task ValidateClaim(Claim claim)
        {
            // Rule 1: Check for duplicate claims in the same month
            var startOfMonth = new DateTime(claim.SubmissionDate.Year, claim.SubmissionDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            
            var duplicateClaim = await _context.Claims
                .Where(c => c.LecturerName == claim.LecturerName
                    && c.SubmissionDate >= startOfMonth
                    && c.SubmissionDate <= endOfMonth
                    && c.Id != claim.Id)
                .FirstOrDefaultAsync();
            
            if (duplicateClaim != null)
            {
                throw new InvalidOperationException(
                    $"A claim for {claim.LecturerName} already exists for {claim.SubmissionDate:MMMM yyyy}. " +
                    "Only one claim per month is allowed.");
            }
            
            // Rule 2: Validate hours don't exceed reasonable monthly limit
            const double maxMonthlyHours = 200; // Reasonable monthly work hours
            if (claim.HoursWorked > maxMonthlyHours)
            {
                throw new InvalidOperationException(
                    $"Hours worked ({claim.HoursWorked}) exceeds the maximum allowed monthly hours ({maxMonthlyHours}).");
            }
            
            // Rule 3: Check for suspicious patterns - very high total amount
            const decimal suspiciousAmount = 50000; // Flag claims over R50,000
            if (claim.TotalAmount > suspiciousAmount)
            {
                // Add a note but don't reject - just flag for manual review
                claim.Notes = (claim.Notes ?? "") + 
                    $" [FLAGGED: High amount - R{claim.TotalAmount:N2} requires additional verification]";
            }
            
            // Rule 4: Validate submission date is not in the future
            if (claim.SubmissionDate > DateTime.Now)
            {
                throw new InvalidOperationException("Submission date cannot be in the future.");
            }
            
            // Rule 5: Validate submission date is not too old (more than 3 months)
            var threeMonthsAgo = DateTime.Now.AddMonths(-3);
            if (claim.SubmissionDate < threeMonthsAgo)
            {
                throw new InvalidOperationException(
                    "Claims older than 3 months cannot be submitted. Please contact administration for assistance.");
            }
        }
        // Approve a claim with tracking
        public async Task ApproveClaim(int id, string approverName, string comments)
        {
            var claim = await GetClaimById(id);
            if (claim == null)
            {
                _loggingService.LogError($"Claim {id} not found for approval");
                throw new InvalidOperationException($"Claim with ID {id} not found.");
            }
            
            if (claim.Status != ClaimStatus.Pending)
            {
                _loggingService.LogWarning($"Attempt to approve non-pending claim {id} by {approverName}");
                throw new InvalidOperationException($"Only pending claims can be approved. Current status: {claim.Status}");
            }
            
            claim.Status = ClaimStatus.Approved;
            claim.ApprovedBy = approverName;
            claim.ApprovedAt = DateTime.Now;
            claim.ApprovalComments = comments;
            
            await _context.SaveChangesAsync();
            
            // Record history
            await AddClaimHistory(id, ClaimStatus.Approved, approverName, comments ?? "Approved", "Approved");
            
            _loggingService.LogInfo($"Claim {id} approved by {approverName} for {claim.LecturerName} - R{claim.TotalAmount:N2}");
        }
        
        // Reject a claim with tracking
        public async Task RejectClaim(int id, string rejectorName, string reason)
        {
            var claim = await GetClaimById(id);
            if (claim == null)
            {
                _loggingService.LogError($"Claim {id} not found for rejection");
                throw new InvalidOperationException($"Claim with ID {id} not found.");
            }
            
            if (claim.Status != ClaimStatus.Pending)
            {
                _loggingService.LogWarning($"Attempt to reject non-pending claim {id} by {rejectorName}");
                throw new InvalidOperationException($"Only pending claims can be rejected. Current status: {claim.Status}");
            }
            
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException("Rejection reason is required.");
            }
            
            claim.Status = ClaimStatus.Rejected;
            claim.RejectedBy = rejectorName;
            claim.RejectedAt = DateTime.Now;
            claim.RejectionReason = reason;
            
            await _context.SaveChangesAsync();
            
            // Record history
            await AddClaimHistory(id, ClaimStatus.Rejected, rejectorName, reason, "Rejected");
            
            _loggingService.LogInfo($"Claim {id} rejected by {rejectorName} for {claim.LecturerName} - Reason: {reason}");
        }
        // Add a document to a claim
        public async Task AddDocument(Document doc)
        {
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
        }
        // Get dashboard statistics
        public DashboardViewModel GetDashboardStats()
        {
            var claims = _context.Claims.ToList();
            var totalAmount = claims.Sum(c => c.TotalAmount);
            
            return new DashboardViewModel
            {
                PendingClaims = claims.Count(c => c.Status == ClaimStatus.Pending),
                TotalClaimed = totalAmount,
                ApprovedClaims = claims.Count(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = claims.Count(c => c.Status == ClaimStatus.Rejected),
                TotalClaims = claims.Count,
                AverageClaimAmount = claims.Any() ? totalAmount / claims.Count : 0,
                RecentClaims = claims.OrderByDescending(c => c.SubmittedAt).Take(5).ToList()
            };
        }
        
        // Check if lecturer has already submitted a claim for a specific month
        public async Task<bool> HasClaimForMonth(string lecturerName, DateTime month)
        {
            var startOfMonth = new DateTime(month.Year, month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            
            return await _context.Claims
                .AnyAsync(c => c.LecturerName == lecturerName
                    && c.SubmissionDate >= startOfMonth
                    && c.SubmissionDate <= endOfMonth);
        }
        
        // Get all claims for a specific lecturer
        public async Task<List<Claim>> GetClaimsByLecturer(string lecturerName)
        {
            return await _context.Claims
                .Include(c => c.Documents)
                .Where(c => c.LecturerName == lecturerName)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }
        
        // Get claim history for audit trail
        public async Task<List<ClaimHistory>> GetClaimHistory(int claimId)
        {
            return await _context.ClaimHistories
                .Where(h => h.ClaimId == claimId)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }
        
        // Private helper to add claim history
        private async Task AddClaimHistory(int claimId, ClaimStatus status, string changedBy, string comments, string action)
        {
            var history = new ClaimHistory
            {
                ClaimId = claimId,
                Status = status,
                ChangedBy = changedBy,
                Comments = comments,
                Action = action,
                ChangedAt = DateTime.Now
            };
            
            _context.ClaimHistories.Add(history);
            await _context.SaveChangesAsync();
        }

    }
}




