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

                TempData["Success"] = $"Welcome back, {user.FullName}!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Login error: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password, UserRole role)
        {
            try
            {
                await _authService.Register(fullName, email, password, role);
                TempData["Success"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Registration error: {ex.Message}";
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
