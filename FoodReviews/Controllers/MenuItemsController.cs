using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodReviews.Models;
using X.PagedList;

namespace FoodReviews.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuItemsController : ControllerBase
    {
        private readonly localdbMSSQLLocalDBContext _context;

        public MenuItemsController(localdbMSSQLLocalDBContext context)
        {
            _context = context;
        }

        // GET: api/MenuItems/{id}/Reviews
        [HttpGet("{id}/Reviews")]
        public async Task<ActionResult<IEnumerable<MenuItemReviewViewModel>>> GetMenuItemReviews(int id,int page = 1)
        {
            try
            {
                var menuItem = await _context.MenuItems
                    .Include(m => m.Reviews)
                        .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(m => m.MenuItemId == id);

                if (menuItem == null)
                {
                    return NotFound($"找不到 ID 為 {id} 的菜單項目");
                }

                var reviews = menuItem.Reviews
                    .OrderByDescending(r => r.ReviewDate)
                    .Select(r => new MenuItemReviewViewModel
                    {
                        ReviewId = r.ReviewId,
                        UserName = r.User.Username,
                        Rating = r.Rating ?? 0,
                        Comment = r.Comment,
                        ReviewDate = r.ReviewDate ?? DateTime.Now
                    })                  
                    .ToList();

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"獲取評論時發生錯誤: {ex.Message}");
            }
        }
    }

    public class MenuItemReviewViewModel
    {
        public int ReviewId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }
} 