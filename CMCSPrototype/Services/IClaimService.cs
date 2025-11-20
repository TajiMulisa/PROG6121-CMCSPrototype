using CMCSPrototype.Data;
using CMCSPrototype.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMCSPrototype.Services
{
    public interface IClaimService
    {
        Task SubmitClaim(Claim claim);
        Task<List<Claim>> GetPendingClaims(string role);
        Task ApproveClaim(int id, string approverName, string comments);
        Task RejectClaim(int id, string rejectorName, string reason);
        Task AddDocument(Document doc);
        DashboardViewModel GetDashboardStats();
        Task<bool> HasClaimForMonth(string lecturerName, int month, int year);
        Task<List<Claim>> GetClaimsByLecturer(string lecturerName);
        Task<Claim?> GetClaimById(int id);
        Task<List<ClaimHistory>> GetClaimHistory(int claimId);
        Task<List<Claim>> GetAllClaims();

    }
}
