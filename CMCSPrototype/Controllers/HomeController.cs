using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CMCSPrototype.Models;
using CMCSPrototype.Services;  // Add this using statement for ClaimService

namespace CMCSPrototype.Controllers;
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IClaimService _claimService;  // Use interface instead of concrete class
    // Update the constructor to inject IClaimService
    public HomeController(ILogger<HomeController> logger, IClaimService claimService)
    {
        _logger = logger;
        _claimService = claimService;  // Assign it here
    }
    // Update the Index action to populate and return the DashboardViewModel
    public IActionResult Index()
    {
        var model = _claimService.GetDashboardStats();  // Fetch stats from the service
        return View(model);  // Pass the model to the view
    }

    public IActionResult Privacy()
    {
        return View();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
