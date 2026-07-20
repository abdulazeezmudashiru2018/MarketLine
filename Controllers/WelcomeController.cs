using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;
using MarketLine.Models;
using MarketLine.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MarketLine.Controllers
{
    public class WelcomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPhotoService _photoService;

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private static readonly string[] VideoExtensions = { ".mp4", ".webm", ".ogg", ".mov" };
        private const long MaxFileBytes = 50 * 1024 * 1024; // Increased to 50 MB

        public WelcomeController(ApplicationDbContext context, IPhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52428800)] // Force ASP.NET to accept up to 50MB requests
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB form size
        public async Task<IActionResult> UploadMedia(string slot, IFormFile file)
        {
            slot = (slot ?? "").Trim().ToLowerInvariant();
            if (slot != "left" && slot != "right")
                return BadRequest(new { message = "Invalid slot." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please choose a file." });

            if (file.Length > MaxFileBytes)
                return BadRequest(new { message = "File must be smaller than 50 MB." });

            var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            string mediaType;
            if (ImageExtensions.Contains(ext)) mediaType = "image";
            else if (VideoExtensions.Contains(ext)) mediaType = "video";
            else return BadRequest(new { message = "Only image (jpg, png, webp, gif) or video (mp4, webm, mov) files are allowed." });

            var uploadResult = await _photoService.UploadMediaAsync(file, "MarketLine/Landing");

            if (uploadResult.Error != null)
            {
                return BadRequest(new { message = uploadResult.Error.Message });
            }

            var media = new LandingMedia
            {
                Slot = slot,
                FilePath = uploadResult.SecureUrl.ToString(),
                MediaType = mediaType,
                UpdatedAt = DateTime.UtcNow
            };

            _context.LandingMediaItems.Add(media);
            await _context.SaveChangesAsync();

            return Ok(new { id = media.Id, slot, mediaType, path = media.FilePath });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.LandingMediaItems.FindAsync(id);
            if (media == null) return NotFound(new { message = "Not found." });

            try
            {
                var uri = new Uri(media.FilePath);
                var pathSegments = uri.AbsolutePath.Split('/');
                var folderIndex = Array.IndexOf(pathSegments, "MarketLine");
                if (folderIndex != -1)
                {
                    var segmentsToKeep = pathSegments.Skip(folderIndex);
                    var fullPublicIdWithExtension = string.Join("/", segmentsToKeep);
                    var publicId = System.IO.Path.ChangeExtension(fullPublicIdWithExtension, null);

                    await _photoService.DeleteMediaAsync(publicId);
                }
            }
            catch { /* Ignore non-fatal cleanup deletion errors */ }

            _context.LandingMediaItems.Remove(media);
            await _context.SaveChangesAsync();

            return Ok(new { id });
        }
    }
}
