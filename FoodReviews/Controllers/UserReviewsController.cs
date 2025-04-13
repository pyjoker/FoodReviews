using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FoodReviews.Models;
using System.Security.Claims;

namespace FoodReviews.Controllers
{
    [Authorize]
    public class UserReviewsController : Controller
    {
        private readonly localdbMSSQLLocalDBContext _context;

        public UserReviewsController(localdbMSSQLLocalDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var reviews = await _context.Reviews
                .Include(r => r.MenuItem)
                    .ThenInclude(m => m.Restaurant)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            return View(reviews);
        }
    }
} 