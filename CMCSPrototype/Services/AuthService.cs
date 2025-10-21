using CMCSPrototype.Data;
using CMCSPrototype.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CMCSPrototype.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ILoggingService _loggingService;

        public AuthService(AppDbContext context, ILoggingService loggingService)
        {
            _context = context;
            _loggingService = loggingService;
        }

        public async Task<User?> Login(string email, string password)
        {
            var user = await GetUserByEmail(email);
            if (user == null || !user.IsActive)
            {
                _loggingService.LogWarning($"Failed login attempt for email: {email}");
                return null;
            }

            if (VerifyPassword(password, user.PasswordHash))
            {
                _loggingService.LogInfo($"User logged in: {user.FullName} ({user.Role})");
                return user;
            }

            _loggingService.LogWarning($"Invalid password for email: {email}");
            return null;
        }

        public async Task<User> Register(string fullName, string email, string password, UserRole role)
        {
            var existingUser = await GetUserByEmail(email);
            if (existingUser != null)
            {
                _loggingService.LogWarning($"Registration attempt with existing email: {email}");
                throw new InvalidOperationException("Email already registered.");
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = role,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _loggingService.LogInfo($"New user registered: {fullName} ({role})");
            return user;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }
    }
}
