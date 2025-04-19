using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FoodReviews.Models;
using System.Security.Claims;
using X.PagedList;

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

        public async Task<IActionResult> Index(int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var reviews = await _context.Reviews
                .Include(r => r.MenuItem)
                    .ThenInclude(m => m.Restaurant)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ReviewDate)
                .ToPagedListAsync(page, 10);
            
            return View(reviews);
        }

        // GET: UserReviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var review = await _context.Reviews
                .Include(r => r.MenuItem)
                    .ThenInclude(m => m.Restaurant)
                .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == userId);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: UserReviews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReviewId,UserId,MenuItemId,RestaurantId,Rating,Comment,ReviewDate")] Review review)
        {
            if (id != review.ReviewId)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (review.UserId != userId)
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    review.ReviewDate = DateTime.Now;
                    _context.Update(review);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.ReviewId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(review);
        }

        // POST: UserReviews/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == userId);

            if (review == null)
            {
                return NotFound();
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.ReviewId == id);
        }
    }
} 