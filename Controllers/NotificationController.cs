using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;

namespace MarketLine.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get unread count (based on cookie tracking)
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var lastSeenId = int.TryParse(Request.Cookies["LastSeenOrderId"], out var v) ? v : 0;

            var orders = await _context.CustomerOrders
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .Select(o => new
                {
                    id = o.Id,
                    name = o.FullName,
                    total = o.Total,
                    date = o.CreatedAt.ToString("MMM dd, HH:mm"),
                    status = o.Status,
                    isNew = o.Id > lastSeenId
                })
                .ToListAsync();

            var unreadCount = orders.Count(o => o.isNew);

            return Json(new { orders, unreadCount });
        }

        [HttpGet]
        public async Task<IActionResult> GetPayments()
        {
            var lastSeenId = int.TryParse(Request.Cookies["LastSeenPaymentId"], out var v) ? v : 0;

            var payments = await _context.CustomerOrders
                .Where(o => o.Status == "Paid")
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .Select(o => new
                {
                    id = o.Id,
                    name = o.FullName,
                    total = o.Total,
                    date = o.CreatedAt.ToString("MMM dd, HH:mm"),
                    method = o.PaymentMethod,
                    isNew = o.Id > lastSeenId
                })
                .ToListAsync();

            var unreadCount = payments.Count(p => p.isNew);

            return Json(new { payments, unreadCount });
        }

        // Called when user opens the notification dropdown
        [HttpPost]
        public async Task<IActionResult> MarkOrdersSeen()
        {
            var latest = await _context.CustomerOrders
                .OrderByDescending(o => o.Id)
                .Select(o => o.Id)
                .FirstOrDefaultAsync();

            Response.Cookies.Append("LastSeenOrderId", latest.ToString(),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30) });

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkPaymentsSeen()
        {
            var latest = await _context.CustomerOrders
                .Where(o => o.Status == "Paid")
                .OrderByDescending(o => o.Id)
                .Select(o => o.Id)
                .FirstOrDefaultAsync();

            Response.Cookies.Append("LastSeenPaymentId", latest.ToString(),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30) });

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.CustomerOrders.FindAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentReceipt(int id)
        {
            var order = await _context.CustomerOrders.FindAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}