using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;

namespace MarketLine.Controllers
{
    public class IdleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IdleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Idle
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            // Uses the same images/videos uploaded from Account/ManageMedia
            var media = await _context.LandingMediaItems
                .AsNoTracking()
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync();

            return View(media);
        }
    }
}