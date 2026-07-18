using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;
using MarketLine.Models;
using System.Text.Json;

namespace MarketLine.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                                         .OrderByDescending(p => p.CreatedAt)
                                         .ToListAsync();
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderSubmission model)
        {
            if (model == null || model.Cart == null || !model.Cart.Any())
                return BadRequest();

            var order = new CustomerOrder
            {
                FullName = model.FullName,
                Email = model.Email,
                ShippingAddress = model.ShippingAddress,
                City = model.City,
                ZipCode = model.ZipCode,
                PaymentMethod = model.PaymentMethod,
                Total = model.Total,
                CartItemsJson = JsonSerializer.Serialize(model.Cart),
                Status = "Paid"   // 👈 both Card and Bank Transfer count as Paid now
            };

            _context.CustomerOrders.Add(order);
            await _context.SaveChangesAsync();

            return Json(new { success = true, orderId = order.Id, message = "Order placed successfully!" });
        }
    }

    public class OrderSubmission
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string ShippingAddress { get; set; } = "";
        public string City { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public decimal Total { get; set; }
        public List<CartItem> Cart { get; set; } = new();
    }

    public class CartItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Qty { get; set; }
    }
}