using CMCSPrototype.Models;

namespace CMCSPrototype.Services
{
    public interface IAuthService
    {
        Task<User?> Login(string email, string password);
        Task<User> Register(string fullName, string email, string password, UserRole role);
        Task<User?> GetUserByEmail(string email);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}
