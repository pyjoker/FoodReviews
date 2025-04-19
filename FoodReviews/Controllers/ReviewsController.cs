using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodReviews.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using X.PagedList;

namespace FoodReviews.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly localdbMSSQLLocalDBContext _context;

        public ReviewsController(localdbMSSQLLocalDBContext context)
        {
            _context = context;
        }

        // GET: api/Reviews/menuItem/5
        [HttpGet("menuItem/{menuItemId}")]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviewsForMenuItem(int menuItemId, int page = 1)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.MenuItemId == menuItemId)
                .OrderByDescending(r => r.ReviewDate)
                //.ToPagedListAsync(page, 2);
                .ToListAsync();

            return reviews;
        }


        // POST: api/Reviews
        [HttpPost]
        public async Task<ActionResult<Review>> CreateReview(Review review)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 獲取當前登入用戶的ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("請先登入後再發表評論");
                }

                int userID = int.Parse(userId);

                // 檢查用戶是否已經評論過這個菜單項目
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.UserId == userID && r.MenuItemId == review.MenuItemId);

                if (existingReview != null)
                {
                    return BadRequest("您已經評論過這個菜單項目，每個菜單項目只能評論一次");
                }

                // 設置評論的用戶ID和日期
                review.UserId = userID;
                review.ReviewDate = DateTime.Now;

                // 確保評分在有效範圍內
                if (review.Rating < 1 || review.Rating > 5)
                {
                    return BadRequest("評分必須在1到5之間");
                }

                // 確保菜單項目存在
                var menuItem = await _context.MenuItems.FindAsync(review.MenuItemId);
                if (menuItem == null)
                {
                    return NotFound("找不到指定的菜單項目");
                }

                // 設置餐廳ID
                review.RestaurantId = menuItem.RestaurantId;

                // 添加評論到資料庫
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // 更新菜單項目的平均評分
                var menuItemReviews = await _context.Reviews
                    .Where(r => r.MenuItemId == review.MenuItemId)
                    .ToListAsync();

                if (menuItemReviews.Any())
                {
                    // 使用 decimal 類型計算平均分
                    decimal averageRating = menuItemReviews.Average(r => (decimal)r.Rating);
                    menuItem.AverageRating = averageRating;
                    menuItem.TotalReviews = menuItemReviews.Count;
                    await _context.SaveChangesAsync();
                }

                // 返回成功響應
                return Ok(new { 
                    success = true, 
                    message = "評論已成功提交",
                    reviewId = review.ReviewId
                });
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                Console.WriteLine($"Error creating review: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // 檢查是否已經保存了評論
                if (review.ReviewId > 0)
                {
                    // 如果評論已經保存，返回成功響應
                    return Ok(new { 
                        success = true, 
                        message = "評論已成功提交，但更新評分時發生錯誤",
                        reviewId = review.ReviewId
                    });
                }
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "保存評論時發生錯誤，請稍後再試",
                    error = ex.Message
                });
            }
        }
    }
} 