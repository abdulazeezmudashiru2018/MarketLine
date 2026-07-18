using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;
using MarketLine.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarketLine.Controllers
{
    public class WelcomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private static readonly string[] VideoExtensions = { ".mp4", ".webm", ".ogg", ".mov" };
        private const long MaxFileBytes = 40 * 1024 * 1024; // 40 MB (video-friendly)

        public WelcomeController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: / -> the public landing page
        public async Task<IActionResult> Index()
        {
            var left = await _context.LandingMediaItems
                .Where(m => m.Slot == "left")
                .OrderBy(m => m.Id)
                .ToListAsync();

            var right = await _context.LandingMediaItems
                .Where(m => m.Slot == "right")
                .OrderBy(m => m.Id)
                .ToListAsync();

            ViewBag.LeftMediaItems = left;
            ViewBag.RightMediaItems = right;

            return View();
        }

        // GET: /Welcome/MediaList?slot=left -> JSON list of slides for the
        // manage panel inside the upload modal.
        [HttpGet]
        public async Task<IActionResult> MediaList(string slot)
        {
            slot = (slot ?? "").Trim().ToLowerInvariant();
            if (slot != "left" && slot != "right")
                return BadRequest(new { message = "Invalid slot." });

            var items = await _context.LandingMediaItems
                .Where(m => m.Slot == slot)
                .OrderBy(m => m.Id)
                .Select(m => new { m.Id, m.FilePath, m.MediaType })
                .ToListAsync();

            return Ok(items);
        }

        // POST: /Welcome/UploadMedia  (multipart/form-data: Slot, File)
        // Adds a new slide to the slot's slideshow (does not replace
        // existing slides — use DeleteMedia to remove old ones).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMedia(string slot, IFormFile file)
        {
            slot = (slot ?? "").Trim().ToLowerInvariant();
            if (slot != "left" && slot != "right")
                return BadRequest(new { message = "Invalid slot." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please choose a file." });

            if (file.Length > MaxFileBytes)
                return BadRequest(new { message = "File must be smaller than 40 MB." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            string mediaType;
            if (ImageExtensions.Contains(ext)) mediaType = "image";
            else if (VideoExtensions.Contains(ext)) mediaType = "video";
            else return BadRequest(new { message = "Only image (jpg, png, webp, gif) or video (mp4, webm, mov) files are allowed." });

            var folder = Path.Combine(_env.WebRootPath, "uploads", "landing");
            Directory.CreateDirectory(folder);

            var fileName = $"{slot}-{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/landing/{fileName}";

            var media = new LandingMedia
            {
                Slot = slot,
                FilePath = relativePath,
                MediaType = mediaType,
                UpdatedAt = DateTime.Now
            };
            _context.LandingMediaItems.Add(media);
            await _context.SaveChangesAsync();

            return Ok(new { id = media.Id, slot, mediaType, path = relativePath });
        }

        // POST: /Welcome/DeleteMedia  (id)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.LandingMediaItems.FindAsync(id);
            if (media == null) return NotFound(new { message = "Not found." });

            var fullPath = Path.Combine(_env.WebRootPath, media.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
            {
                try { System.IO.File.Delete(fullPath); } catch { /* non-fatal cleanup */ }
            }

            _context.LandingMediaItems.Remove(media);
            await _context.SaveChangesAsync();

            return Ok(new { id });
        }
    }
}