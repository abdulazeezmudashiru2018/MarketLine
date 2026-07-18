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
    public class GoodsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

        public GoodsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Goods  -> the gallery page
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // GET: /Goods/Search?q=coke  -> JSON list for the Sales invoice
        // "Goods" drawer/search box. Returns everything when q is empty.
        [HttpGet]
        public async Task<IActionResult> Search(string? q)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(p => EF.Functions.Like(p.Name, $"%{term}%"));
            }

            var results = await query
                .OrderBy(p => p.Name)
                .Take(50)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    ImagePath = p.ImagePath,
                    Barcode = p.Barcode
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: /Goods/ByBarcode?code=012345678905  -> used by the camera
        // scanner on the Sales invoice page to auto-add a scanned item.
        [HttpGet]
        public async Task<IActionResult> ByBarcode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { message = "No barcode provided." });

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == code);

            if (product == null)
                return NotFound(new { message = "No item matches this barcode." });

            return Ok(ProductDto.FromEntity(product));
        }

        // POST: /Goods/Create  (multipart/form-data: Name, Price, Barcode, Image)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] ProductFormInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                return BadRequest(new { message = "Item name is required." });

            if (input.Price < 0)
                return BadRequest(new { message = "Price cannot be negative." });

            string? imagePath = null;
            if (input.Image != null)
            {
                var (ok, error, savedPath) = await SaveImageAsync(input.Image);
                if (!ok) return BadRequest(new { message = error });
                imagePath = savedPath;
            }

            var product = new Product
            {
                Name = input.Name.Trim(),
                Price = input.Price,
                ImagePath = imagePath,
                Barcode = string.IsNullOrWhiteSpace(input.Barcode) ? null : input.Barcode.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(ProductDto.FromEntity(product));
        }

        // POST: /Goods/Edit  (multipart/form-data: Id, Name, Price, Barcode, Image?)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] ProductFormInput input)
        {
            var product = await _context.Products.FindAsync(input.Id);
            if (product == null) return NotFound(new { message = "Item not found." });

            if (string.IsNullOrWhiteSpace(input.Name))
                return BadRequest(new { message = "Item name is required." });

            if (input.Price < 0)
                return BadRequest(new { message = "Price cannot be negative." });

            product.Name = input.Name.Trim();
            product.Price = input.Price;
            product.Barcode = string.IsNullOrWhiteSpace(input.Barcode) ? null : input.Barcode.Trim();

            if (input.Image != null)
            {
                var (ok, error, savedPath) = await SaveImageAsync(input.Image);
                if (!ok) return BadRequest(new { message = error });

                DeletePhysicalImage(product.ImagePath);
                product.ImagePath = savedPath;
            }

            await _context.SaveChangesAsync();

            return Ok(ProductDto.FromEntity(product));
        }

        // POST: /Goods/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Item not found." });

            DeletePhysicalImage(product.ImagePath);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { id });
        }

        // ---- helpers ----------------------------------------------------

        private async Task<(bool ok, string? error, string? path)> SaveImageAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return (false, "Only JPG, PNG, WEBP or GIF images are allowed.", null);

            if (file.Length > MaxImageBytes)
                return (false, "Image must be smaller than 5 MB.", null);

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "goods");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, null, $"/uploads/goods/{fileName}");
        }

        private void DeletePhysicalImage(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
            {
                try { System.IO.File.Delete(fullPath); } catch { /* non-fatal cleanup */ }
            }
        }
    }
}