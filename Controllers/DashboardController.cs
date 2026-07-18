using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;
using MarketLine.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MarketLine.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Existing physical/in-shop sales
            var existingSales = await _context.Sales.ToListAsync();

            // Existing online orders
            var customerOrders = await _context.CustomerOrders.ToListAsync();

            // Used by existing cards and recent sales section
            var convertedOrders = customerOrders.Select(o => new Sale
            {
                Id = o.Id + 10000,
                CustomerName = o.FullName,
                Amount = o.Total,
                SaleDate = o.CreatedAt
            }).ToList();

            var allSales = existingSales
                .Concat(convertedOrders)
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .ToList();

            /*
             * BONUS WHEEL DATA:
             * Combines shop purchases and online purchases.
             * Groups customers by name, adds their totals,
             * ranks them, then keeps only the first 12.
             */
            var bonusPurchases = existingSales
                .Select(s => new BonusPurchase
                {
                    CustomerName = CleanName(s.CustomerName),
                    Amount = s.Amount,
                    PurchasedAt = s.SaleDate,
                    Source = "Shop",
                    Email = string.Empty,
                    ShippingAddress = string.Empty,
                    City = string.Empty,
                    PaymentMethod = string.Empty
                })
                .Concat(customerOrders.Select(o => new BonusPurchase
                {
                    CustomerName = CleanName(o.FullName),
                    Amount = o.Total,
                    PurchasedAt = o.CreatedAt,
                    Source = "Online Order",
                    Email = o.Email ?? string.Empty,
                    ShippingAddress = o.ShippingAddress ?? string.Empty,
                    City = o.City ?? string.Empty,
                    PaymentMethod = o.PaymentMethod ?? string.Empty
                }))
                .Where(p => !string.IsNullOrWhiteSpace(p.CustomerName))
                .ToList();

            var topTwelve = bonusPurchases
                .GroupBy(p => p.CustomerName, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var latestPurchase = group
                        .OrderByDescending(p => p.PurchasedAt)
                        .First();

                    // Gets the latest online order so we can display contact/order details.
                    var latestOnlineOrder = group
                        .Where(p => p.Source == "Online Order")
                        .OrderByDescending(p => p.PurchasedAt)
                        .FirstOrDefault();

                    return new TopCustomerDto
                    {
                        Name = group.First().CustomerName,
                        TotalSpent = group.Sum(p => p.Amount),
                        PurchaseCount = group.Count(),

                        ShopPurchaseCount = group.Count(p => p.Source == "Shop"),
                        ShopPurchaseTotal = group
                            .Where(p => p.Source == "Shop")
                            .Sum(p => p.Amount),

                        OnlineOrderCount = group.Count(p => p.Source == "Online Order"),
                        OnlineOrderTotal = group
                            .Where(p => p.Source == "Online Order")
                            .Sum(p => p.Amount),

                        LastPurchaseDate = latestPurchase.PurchasedAt,
                        LatestPurchaseSource = latestPurchase.Source,

                        Email = ValueOrNotAvailable(latestOnlineOrder?.Email),
                        ShippingAddress = ValueOrNotAvailable(latestOnlineOrder?.ShippingAddress),
                        City = ValueOrNotAvailable(latestOnlineOrder?.City),
                        PaymentMethod = ValueOrNotAvailable(latestOnlineOrder?.PaymentMethod)
                    };
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(12)
                .ToList();

            // Give each selected bonus customer a rank from 1 to 12.
            for (var i = 0; i < topTwelve.Count; i++)
            {
                topTwelve[i].Rank = i + 1;
            }

            var viewModel = new DashboardViewModel
            {
                HighestSale = allSales.Any() ? allSales.Max(s => s.Amount) : 0m,
                LowestSale = allSales.Any() ? allSales.Min(s => s.Amount) : 0m,
                TotalSales = allSales.Sum(s => s.Amount),
                RecentSales = allSales.Take(4).ToList(),

                NotificationCount = await _context.CustomerOrders.CountAsync(),

                TrendPoints = allSales
                    .OrderBy(s => s.SaleDate)
                    .ThenBy(s => s.Id)
                    .Select(s => s.Amount)
                    .ToList(),

                TopCustomers = topTwelve
            };

            return View(viewModel);
        }

        private static string CleanName(string? name)
        {
            return string.Join(
                " ",
                (name ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string ValueOrNotAvailable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Not available"
                : value.Trim();
        }

        /*
         * This is an in-memory helper only.
         * It does NOT create a database table.
         */
        private sealed class BonusPurchase
        {
            public string CustomerName { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime PurchasedAt { get; set; }
            public string Source { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;
            public string ShippingAddress { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string PaymentMethod { get; set; } = string.Empty;
        }
    }
}