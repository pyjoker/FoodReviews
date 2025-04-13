using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodReviews.Models;

namespace FoodReviews.Controllers
{
    [Authorize] // 只有登入用戶才能訪問
    public class UserController : Controller
    {
        private readonly localdbMSSQLLocalDBContext _context;

        public UserController(localdbMSSQLLocalDBContext context)
        {
            _context = context;
        }

        // GET: User/MyReviews
        public async Task<IActionResult> MyReviews()
        {
            // 獲取當前登入用戶的 ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(userIdClaim.Value);

            // 獲取用戶的所有評論，包括餐廳和菜單項目的評論
            var restaurantReviews = await _context.Reviews
                .Where(r => r.UserId == userId && r.RestaurantId != null)
                .Include(r => r.Restaurant)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            var menuItemReviews = await _context.Reviews
                .Where(r => r.UserId == userId && r.MenuItemId != null)
                .Include(r => r.MenuItem)
                .ThenInclude(m => m.Restaurant)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            // 將評論分為餐廳評論和菜單項目評論
            var viewModel = new MyReviewsViewModel
            {
                RestaurantReviews = restaurantReviews,
                MenuItemReviews = menuItemReviews
            };

            return View(viewModel);
        }
    }

    // 用於顯示用戶評論的視圖模型
    public class MyReviewsViewModel
    {
        public List<Review> RestaurantReviews { get; set; }
        public List<Review> MenuItemReviews { get; set; }
    }
} 