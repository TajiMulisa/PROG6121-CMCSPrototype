using CMCSPrototype.Models;
using CMCSPrototype.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMCSPrototype.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var user = await _authService.Login(email, password);
                if (user == null)
                {
                    TempData["Error"] = "Invalid email or password.";
                    return View();
                }

                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserRole", user.Role.ToString());
                HttpContext.Session.SetString("UserEmail", user.Email);

                TempData["Success"] = $"Welcome back, {user.FullName}!";
                
                // Redirect based on role
                if (user.Role == UserRole.HR)
                {
                    return RedirectToAction("Index", "HR");
                }
                
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Login error: {ex.Message}";
                return View();
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Logged out successfully.";
            return RedirectToAction("Login");
        }
    }
}
