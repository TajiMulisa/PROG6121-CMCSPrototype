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
        // Get all pending claims for approval based on role
        public async Task<List<Claim>> GetPendingClaims(string role)
        {
            if (role == "Coordinator")
            {
                return await _context.Claims
                    .Include(c => c.Documents)
                    .Where(c => c.Status == ClaimStatus.Pending)
                    .OrderByDescending(c => c.SubmittedAt)
                    .ToListAsync();
            }
            else if (role == "Manager")
            {
                return await _context.Claims
                    .Include(c => c.Documents)
                    .Where(c => c.Status == ClaimStatus.Verified)
                    .OrderByDescending(c => c.SubmittedAt)
                    .ToListAsync();
            }
            return new List<Claim>(); // Return empty list for other roles
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
            var duplicateClaim = await _context.Claims
                .Where(c => c.LecturerName == claim.LecturerName
                    && c.ClaimMonth == claim.ClaimMonth
                    && c.ClaimYear == claim.ClaimYear
                    && c.Id != claim.Id)
                .FirstOrDefaultAsync();
            
            if (duplicateClaim != null)
            {
                throw new InvalidOperationException(
                    $"A claim for {claim.LecturerName} already exists for {new DateTime(claim.ClaimYear, claim.ClaimMonth, 1):MMMM yyyy}. " +
                    "Only one claim per month is allowed.");
            }
            
            // Rule 2: Validate hours don't exceed reasonable monthly limit
            const double maxMonthlyHours = 180; // Reasonable monthly work hours
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
        public async Task ApproveClaim(int id, string approverRole, string comments)
        {
            var claim = await GetClaimById(id);
            if (claim == null)
            {
                _loggingService.LogError($"Claim {id} not found for approval");
                throw new InvalidOperationException($"Claim with ID {id} not found.");
            }

            // Coordinator approval - first stage (Verification)
            if (approverRole == "Coordinator")
            {
                if (claim.Status != ClaimStatus.Pending)
                {
                    _loggingService.LogWarning($"Attempt to verify non-pending claim {id} by Coordinator");
                    throw new InvalidOperationException($"Claim must be in Pending status for Coordinator verification. Current status: {claim.Status}");
                }

                claim.CoordinatorApprovedBy = approverRole;
                claim.CoordinatorApprovedAt = DateTime.Now;
                claim.CoordinatorApprovalComments = comments;
                claim.Status = ClaimStatus.Verified;

                await AddClaimHistory(id, ClaimStatus.Verified, approverRole, comments ?? "Verified by Coordinator", "Coordinator Verified");
                _loggingService.LogInfo($"Claim {id} verified by Coordinator for {claim.LecturerName} - R{claim.TotalAmount:N2}");
            }
            // Manager approval - second stage (Final Approval)
            else if (approverRole == "Manager")
            {
                if (claim.Status != ClaimStatus.Verified)
                {
                    _loggingService.LogWarning($"Attempt to approve claim {id} by Manager without Coordinator verification");
                    throw new InvalidOperationException($"Claim must be verified by Coordinator first. Current status: {claim.Status}");
                }

                claim.ManagerApprovedBy = approverRole;
                claim.ManagerApprovedAt = DateTime.Now;
                claim.ManagerApprovalComments = comments;
                claim.Status = ClaimStatus.Approved;

                await AddClaimHistory(id, ClaimStatus.Approved, approverRole, comments ?? "Approved by Manager", "Manager Approved - Final");
                _loggingService.LogInfo($"Claim {id} fully approved by Manager for {claim.LecturerName} - R{claim.TotalAmount:N2}");
            }
            else
            {
                throw new InvalidOperationException($"Invalid approver role: {approverRole}");
            }

            await _context.SaveChangesAsync();
        }
        
        // Reject a claim with tracking
        public async Task RejectClaim(int id, string rejectorRole, string reason)
        {
            var claim = await GetClaimById(id);
            if (claim == null)
            {
                _loggingService.LogError($"Claim {id} not found for rejection");
                throw new InvalidOperationException($"Claim with ID {id} not found.");
            }
            
            if (claim.Status == ClaimStatus.Approved || claim.Status == ClaimStatus.Rejected)
            {
                _loggingService.LogWarning($"Attempt to reject already finalized claim {id} by {rejectorRole}");
                throw new InvalidOperationException($"Cannot reject a claim with status: {claim.Status}");
            }
            
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException("Rejection reason is required.");
            }
            
            claim.Status = ClaimStatus.Rejected;
            claim.RejectedBy = rejectorRole;
            claim.RejectedAt = DateTime.Now;
            claim.RejectionReason = reason;
            
            await _context.SaveChangesAsync();
            
            // Record history
            await AddClaimHistory(id, ClaimStatus.Rejected, rejectorRole, reason, "Rejected");
            
            _loggingService.LogInfo($"Claim {id} rejected by {rejectorRole} for {claim.LecturerName} - Reason: {reason}");
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
        public async Task<bool> HasClaimForMonth(string lecturerName, int month, int year)
        {
            return await _context.Claims
                .AnyAsync(c => c.LecturerName == lecturerName
                    && c.ClaimMonth == month
                    && c.ClaimYear == year);
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

        // Get all claims (for HR dashboard)
        public async Task<List<Claim>> GetAllClaims()
        {
            return await _context.Claims
                .Include(c => c.Documents)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();
        }

    }
}




