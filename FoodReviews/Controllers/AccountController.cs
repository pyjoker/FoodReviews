using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodReviews.Models;
using System.Text;
using System.Security.Cryptography;

namespace FoodReviews.Controllers
{
    public class AccountController : Controller
    {
        private readonly localdbMSSQLLocalDBContext _context;

        public AccountController(localdbMSSQLLocalDBContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "請輸入用戶名和密碼");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || user.Password != password) // 注意：實際應用中應該使用加密密碼
            {
                ModelState.AddModelError("", "用戶名或密碼不正確");
                return View();
            }

            // 創建身份驗證 cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "所有欄位都必須填寫");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "密碼和確認密碼不匹配");
                return View();
            }

            // 檢查用戶名是否已存在
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("", "用戶名已被使用");
                return View();
            }

            // 檢查電子郵件是否已存在
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("", "電子郵件已被使用");
                return View();
            }
            if (password.Length < 6)
            {
                ModelState.AddModelError("", "密碼至少需要 6 個字元");
                return View();
            }
            // 創建新用戶
            //var user = new User
            //{
            //    Username = username,
            //    Email = email,
            //    Password = password, // 注意：實際應用中應該加密密碼
            //    RegisterDate = DateTime.Now
            //};

            //_context.Users.Add(user);
            //await _context.SaveChangesAsync();
            // ✅ 密碼雜湊（SHA256）
            string hashedPassword = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(password)));

            // 創建新用戶
            var user = new User
            {
                Username = username,
                Email = email,
                Password = hashedPassword,
                RegisterDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            // 自動登入
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
} 