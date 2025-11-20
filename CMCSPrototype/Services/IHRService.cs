using CMCSPrototype.Models;

namespace CMCSPrototype.Services
{
    public interface IHRService
    {
        // User Management
        Task<User> CreateUser(string fullName, string email, string password, UserRole role, decimal? hourlyRate = null);
        Task<User?> GetUserById(int id);
        Task<List<User>> GetAllUsers();
        Task<List<User>> GetLecturers();
        Task<User> UpdateUser(int id, string fullName, string email, UserRole role, decimal? hourlyRate, bool isActive);
        Task<bool> UpdateUserHourlyRate(int userId, decimal hourlyRate);
        Task<bool> DeactivateUser(int userId);
        Task<bool> ActivateUser(int userId);
        
        // Report Generation
        Task<List<Claim>> GetApprovedClaimsForReport(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, decimal>> GetLecturerPaymentSummary(DateTime? startDate = null, DateTime? endDate = null);
    }
}
