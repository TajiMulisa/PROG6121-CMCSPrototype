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
        public ClaimService(AppDbContext context)
        {
            _context = context;
        }
        // Get all pending claims for approval
        public async Task<List<Claim>> GetPendingClaims()
        {
            return await _context.Claims.Where(c => c.Status == ClaimStatus.Pending).ToListAsync();
        }
        // Get a specific claim by ID (including documents)
        public async Task<Claim> GetClaimById(int id)
        {
            return await _context.Claims.Include(c => c.Documents).FirstOrDefaultAsync(c => c.Id == id);
        }
        // Submit a new claim
        public async Task SubmitClaim(Claim claim)
        {
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
        }
        // Approve a claim
        public async Task ApproveClaim(int id)
        {
            var claim = await GetClaimById(id);
            if (claim != null)
            {
                claim.Status = ClaimStatus.Approved;
                await _context.SaveChangesAsync();
            }
        }
        // Reject a claim
        public async Task RejectClaim(int id)
        {
            var claim = await GetClaimById(id);
            if (claim != null)
            {
                claim.Status = ClaimStatus.Rejected;
                await _context.SaveChangesAsync();
            }
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
            return new DashboardViewModel
            {
                PendingClaims = _context.Claims.Count(c => c.Status == ClaimStatus.Pending),
                TotalClaimed = _context.Claims.Sum(c => (decimal)c.HoursWorked * c.HourlyRate),
                ApprovedClaims = _context.Claims.Count(c => c.Status == ClaimStatus.Approved)
            };
        }

    }
}

