using CMCSPrototype.Data;
using CMCSPrototype.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMCSPrototype.Services
{
    public interface IClaimService
    {
        Task SubmitClaim(Claim claim);
        Task<List<Claim>> GetPendingClaims();
        Task ApproveClaim(int id);
        Task RejectClaim(int id);
        Task AddDocument(Document doc);
        DashboardViewModel GetDashboardStats();
        Task<bool> HasClaimForMonth(string lecturerName, DateTime month);
        Task<List<Claim>> GetClaimsByLecturer(string lecturerName);
    }
}
