using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;
using MarketLine.Models;

namespace MarketLine.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            var media = await _context.LandingMediaItems
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync();
            return View(media);
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Please enter both email and password.";
                return RedirectToAction("Login");
            }

            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == username && u.Password == password);

            if (user == null)
            {
                TempData["Error"] = "Invalid email or password.";
                return RedirectToAction("Login");
            }

            HttpContext.Session.SetString("User", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetInt32("UserId", user.Id);

            return RedirectToAction("Index", "Dashboard");
        }

        // Check if registration limit is reached (API endpoint for AJAX check)
        [HttpGet]
        public async Task<IActionResult> CheckRegistrationLimit()
        {
            var count = await _context.UserAccounts.CountAsync();
            return Json(new { limitReached = count >= 3 });
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            // Server-side guard to prevent direct URL access when limit is reached
            var count = await _context.UserAccounts.CountAsync();
            if (count >= 3)
            {
                TempData["Error"] = "Registration limit reached. No more accounts can be created.";
                return RedirectToAction("Login");
            }

            var media = await _context.LandingMediaItems
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync();
            return View(media);
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserAccount model, string confirmPassword, string? agreeTerms)
        {
            var count = await _context.UserAccounts.CountAsync();
            if (count >= 3)
            {
                TempData["Error"] = "Registration limit exceeded. Account creation blocked.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(agreeTerms))
            {
                TempData["Error"] = "You must agree to the Terms of Service.";
                return RedirectToAction("Register");
            }

            if (model.Password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction("Register");
            }

            var exists = await _context.UserAccounts.AnyAsync(u => u.Email == model.Email);
            if (exists)
            {
                TempData["Error"] = "This email is already registered.";
                return RedirectToAction("Register");
            }

            _context.UserAccounts.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Account created successfully! Please log in.";
            return RedirectToAction("Login");
        }

        // FORGOT PASSWORD STEP 1: Verify user exists by Email OR Phone
        [HttpPost]
        public async Task<IActionResult> VerifyForgotPassword([FromBody] ForgotVerificationDto model)
        {
            if (model == null || (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Phone)))
            {
                return Json(new { success = false, message = "Please fill in the details." });
            }

            // Matches if EITHER the email OR phone number matches an existing user account
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => (!string.IsNullOrEmpty(model.Email) && u.Email == model.Email) ||
                                          (!string.IsNullOrEmpty(model.Phone) && u.PhoneNumber == model.Phone));

            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            return Json(new { success = true, userId = user.Id });
        }

        // FORGOT PASSWORD STEP 2: Reset user's password
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.NewPassword))
            {
                return Json(new { success = false, message = "Invalid data submitted." });
            }

            var user = await _context.UserAccounts.FindAsync(model.UserId);
            if (user == null)
            {
                return Json(new { success = false, message = "User verification failed." });
            }

            user.Password = model.NewPassword;
            _context.UserAccounts.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> ManageMedia()
        {
            var media = await _context.LandingMediaItems
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync();
            return View(media);
        }

        [HttpPost]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction("ManageMedia");
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var ext = Path.GetExtension(file.FileName).ToLower();
            var isVideo = new[] { ".mp4", ".webm", ".ogg", ".mov" }.Contains(ext);

            var media = new LandingMedia
            {
                Slot = "login",
                FilePath = $"/uploads/{uniqueName}",
                MediaType = isVideo ? "video" : "image",
                UpdatedAt = DateTime.Now
            };

            _context.LandingMediaItems.Add(media);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Media uploaded successfully!";
            return RedirectToAction("ManageMedia");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.LandingMediaItems.FindAsync(id);
            if (media != null)
            {
                var fullPath = Path.Combine(_env.WebRootPath, media.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                _context.LandingMediaItems.Remove(media);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageMedia");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }


        [HttpGet]
        public async Task<IActionResult> Profiles()
        {
            // Check if user is logged in (optional security)
            if (HttpContext.Session.GetString("User") == null)
            {
                return RedirectToAction("Login");
            }

            var users = await _context.UserAccounts
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }

    }

    // Helper dtos to prevent JSON parsing issues
    public class ForgotVerificationDto
    {
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public int UserId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }

}