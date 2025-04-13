using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FoodReviews.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodReviews.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly localdbMSSQLLocalDBContext _context;

    public HomeController(ILogger<HomeController> logger, localdbMSSQLLocalDBContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Search(string searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            return RedirectToAction(nameof(Index));
        }

        // Search for a restaurant by name
        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.RestaurantName.Contains(searchQuery));

        if (restaurant != null)
        {
            // Redirect to the restaurant details page
            return RedirectToAction("Details", "Restaurants", new { id = restaurant.RestaurantId });
        }
        else
        {
            // If no restaurant is found, redirect to the restaurants index with the search query
            return RedirectToAction("Index", "Restaurants", new { searchString = searchQuery });
        }
    }

    // API endpoint for restaurant suggestions
    [HttpGet]
    public async Task<IActionResult> GetRestaurantSuggestions(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return Json(new List<object>());
        }

        var suggestions = await _context.Restaurants
            .Where(r => r.RestaurantName.Contains(query))
            .Select(r => new {
                id = r.RestaurantId,
                name = r.RestaurantName,
                address = r.Address,
                rating = r.AverageRating
            })
            .Take(5)
            .ToListAsync();

        return Json(suggestions);
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