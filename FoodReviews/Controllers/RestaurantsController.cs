using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FoodReviews.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using X.PagedList;
using X.PagedList.Mvc.Core;

namespace FoodReviews.Controllers
{
    public class RestaurantsController : Controller
    {
        private readonly localdbMSSQLLocalDBContext _context;

        public RestaurantsController(localdbMSSQLLocalDBContext context)
        {
            _context = context;
        }

        // GET: Restaurants
        public async Task<IActionResult> Index(string searchString, string address, decimal? minRating, int page = 1)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["AddressFilter"] = address;
            ViewData["RatingFilter"] = minRating;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = false;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(int.Parse(userId));
                isAdmin = user?.IsAdmin ?? false;
            }
            ViewBag.IsAdmin = isAdmin;

            var restaurants = from r in _context.Restaurants
                            select r;

            if (!string.IsNullOrEmpty(searchString))
            {
                restaurants = restaurants.Where(r => r.RestaurantName.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(address))
            {
                restaurants = restaurants.Where(r => r.Address.Contains(address));
            }

            if (minRating.HasValue)
            {
                restaurants = restaurants.Where(r => r.AverageRating >= minRating.Value);
            }
            return View(await restaurants.ToPagedListAsync(page, 10)); 
            //return View(await restaurants.ToListAsync());
        }

        // GET: Restaurants/Details/5
        public async Task<IActionResult> Details(int? id, string searchString, int page = 1)
        {
            ViewData["CurrentFilter"] = searchString;
            if (id == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = false;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(int.Parse(userId));
                isAdmin = user?.IsAdmin ?? false;
            }
            ViewBag.IsAdmin = isAdmin;
            // 獲取餐廳詳細信息，包括菜單項目和評論
            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems)
                    .ThenInclude(m => m.Reviews)
                        .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.RestaurantId == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            // 獲取所有菜單項目類別
            var categories = restaurant.MenuItems
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // 初始化所有類別為選中狀態
            var selectedCategories = categories.ToDictionary(c => c, c => true);

            ViewBag.Categories = categories;
            ViewBag.SelectedCategories = selectedCategories;
            // 查詢該餐廳的菜單項目
            var menuItems = _context.MenuItems
                .Where(m => m.RestaurantId == id);

            // 根據名稱搜尋
            if (!string.IsNullOrEmpty(searchString))
            {
                menuItems = menuItems.Where(m => m.ItemName.Contains(searchString));
            }

            ViewData["MenuItems"] = await menuItems.ToListAsync();

            return View(restaurant);
        }

        // GET: Restaurants/Create
        public IActionResult Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.Find(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        // POST: Restaurants/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RestaurantId,RestaurantName,Address,Phone,OpeningHours,AverageRating,TotalReviews")] Restaurant restaurant)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                _context.Add(restaurant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(restaurant);
        }

        // GET: Restaurants/CreateFood
        public IActionResult CreateFood(int restaurantId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var menuItem = new MenuItem { RestaurantId = restaurantId };
            var user = _context.Users.Find(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Details", "Restaurants", new { id = menuItem.RestaurantId });
            }
            
            return View(menuItem);
        }

        // POST: Restaurants/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFood([Bind("RestaurantId,ItemName,Category,Price,Description")] MenuItem menuItem)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var restaurant = await _context.Restaurants.FindAsync(menuItem.RestaurantId);
            if (restaurant == null)
            {
                ModelState.AddModelError("RestaurantId", "餐廳不存在。");
                return View(menuItem);
            }
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Details", "Restaurants", new { id = menuItem.RestaurantId });
            }

            if (ModelState.IsValid)
            {
                _context.Add(menuItem);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Restaurants", new { id = menuItem.RestaurantId });
            }
            return View(menuItem);
        }

        // GET: Restaurants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Index");
            }

            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null)
            {
                return NotFound();
            }
            return View(restaurant);
        }

        // POST: Restaurants/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RestaurantId,RestaurantName,Address,Phone,OpeningHours,AverageRating,TotalReviews")] Restaurant restaurant)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Index");
            }

            if (id != restaurant.RestaurantId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(restaurant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestaurantExists(restaurant.RestaurantId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(restaurant);
        }
        // GET: Restaurants/EditFood/5
        public async Task<IActionResult> EditFood(int? id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Index");
            }

            if (id == null)
            {
                return NotFound();
            }

            var menuItems = await _context.MenuItems.FindAsync(id);
            if (menuItems == null)
            {
                return NotFound();
            }
            return View(menuItems);
        }

        // POST: Restaurants/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFood(int id, [Bind("MenuItemId,RestaurantId,ItemName,Category,Price,Description,AverageRating,TotalReviews")] MenuItem menuItem)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Details");
            }

            if (id != menuItem.MenuItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingItem = await _context.MenuItems.FindAsync(id);
                if (existingItem == null)
                    return NotFound();

                // 僅更新允許的欄位，保留其他欄位原值
                existingItem.ItemName = menuItem.ItemName;
                existingItem.Category = menuItem.Category;
                existingItem.Price = menuItem.Price;
                existingItem.Description = menuItem.Description;

                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine("儲存成功");
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }

                return RedirectToAction("Details", new { id = existingItem.RestaurantId });
            }
            return View(menuItem);
        }

        // GET: Restaurants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Index");
            }

            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(m => m.RestaurantId == id);
            if (restaurant == null)
            {
                return NotFound();
            }

            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Index");
            }

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant != null)
            {
                _context.Restaurants.Remove(restaurant);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
   
        // GET: Restaurants/DeleteFood/5
        public async Task<IActionResult> DeleteFood(int? id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var menuitem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.MenuItemId == id);
            if (menuitem == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Details", "Restaurants", new { id = menuitem.RestaurantId });
            }

            return View(menuitem);
        }

        // POST: Restaurants/DeleteFood/5
        [HttpPost, ActionName("DeleteFood")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFoodConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Index");
            }

            var menuitem = await _context.MenuItems.FindAsync(id);
            if (menuitem != null)
            {
                _context.MenuItems.Remove(menuitem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Restaurants", new { id = menuitem.RestaurantId });
        }
        private bool RestaurantExists(int id)
        {
            return _context.Restaurants.Any(e => e.RestaurantId == id);
        }
    }
}
