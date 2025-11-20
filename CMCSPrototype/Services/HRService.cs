using CMCSPrototype.Data;
using CMCSPrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCSPrototype.Services
{
    public class HRService : IHRService
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILoggingService _loggingService;

        public HRService(AppDbContext context, IAuthService authService, ILoggingService loggingService)
        {
            _context = context;
            _authService = authService;
            _loggingService = loggingService;
        }

        // User Management
        public async Task<User> CreateUser(string fullName, string email, string password, UserRole role, decimal? hourlyRate = null)
        {
            // Check if email already exists
            var existingUser = await _authService.GetUserByEmail(email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            // Validate hourly rate for lecturers
            if (role == UserRole.Lecturer && (!hourlyRate.HasValue || hourlyRate.Value < 50 || hourlyRate.Value > 1000))
            {
                throw new InvalidOperationException("Hourly rate is required for lecturers and must be between R50 and R1000.");
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = _authService.HashPassword(password),
                Role = role,
                HourlyRate = role == UserRole.Lecturer ? hourlyRate : null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _loggingService.LogInfo($"HR created new user: {fullName} ({role}) with email {email}");
            return user;
        }

        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<List<User>> GetAllUsers()
        {
            return await _context.Users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<List<User>> GetLecturers()
        {
            return await _context.Users
                .Where(u => u.Role == UserRole.Lecturer)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User> UpdateUser(int id, string fullName, string email, UserRole role, decimal? hourlyRate, bool isActive)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            // Check if email is being changed and if it's already in use
            if (user.Email != email)
            {
                var existingUser = await _authService.GetUserByEmail(email);
                if (existingUser != null && existingUser.Id != id)
                {
                    throw new InvalidOperationException("Email is already in use by another user.");
                }
            }

            // Validate hourly rate for lecturers
            if (role == UserRole.Lecturer && (!hourlyRate.HasValue || hourlyRate.Value < 50 || hourlyRate.Value > 1000))
            {
                throw new InvalidOperationException("Hourly rate is required for lecturers and must be between R50 and R1000.");
            }

            user.FullName = fullName;
            user.Email = email;
            user.Role = role;
            user.HourlyRate = role == UserRole.Lecturer ? hourlyRate : null;
            user.IsActive = isActive;

            await _context.SaveChangesAsync();

            _loggingService.LogInfo($"HR updated user: {fullName} (ID: {id})");
            return user;
        }

        public async Task<bool> UpdateUserHourlyRate(int userId, decimal hourlyRate)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (user.Role != UserRole.Lecturer)
            {
                throw new InvalidOperationException("Only lecturers have hourly rates.");
            }

            if (hourlyRate < 50 || hourlyRate > 1000)
            {
                throw new InvalidOperationException("Hourly rate must be between R50 and R1000.");
            }

            user.HourlyRate = hourlyRate;
            await _context.SaveChangesAsync();

            _loggingService.LogInfo($"HR updated hourly rate for {user.FullName} to R{hourlyRate}");
            return true;
        }

        public async Task<bool> DeactivateUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            _loggingService.LogInfo($"HR deactivated user: {user.FullName} (ID: {userId})");
            return true;
        }

        public async Task<bool> ActivateUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            _loggingService.LogInfo($"HR activated user: {user.FullName} (ID: {userId})");
            return true;
        }

        // Report Generation
        public async Task<List<Claim>> GetApprovedClaimsForReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Claims
                .Include(c => c.Documents)
                .Where(c => c.Status == ClaimStatus.Approved);

            if (startDate.HasValue)
            {
                query = query.Where(c => c.SubmissionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.SubmissionDate <= endDate.Value);
            }

            return await query
                .OrderBy(c => c.LecturerName)
                .ThenBy(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetLecturerPaymentSummary(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved);

            if (startDate.HasValue)
            {
                query = query.Where(c => c.SubmissionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.SubmissionDate <= endDate.Value);
            }

            var claims = await query.ToListAsync();

            return claims
                .GroupBy(c => c.LecturerName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(c => c.TotalAmount)
                );
        }
    }
}
