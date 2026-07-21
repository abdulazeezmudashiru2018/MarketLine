using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;
using MarketLine.Models;
using System;
using System.Collections.Generic;
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

        // ============================================================
        // NEW: API endpoints for Dashboard popup buttons
        // (Highest, Lowest, Total Breakdown, Daily Sales)
        // ============================================================

        // GET: /Dashboard/GetTotalSalesBreakdown
        [HttpGet]
        public async Task<IActionResult> GetTotalSalesBreakdown()
        {
            var sales = await _context.Sales.ToListAsync();
            var invoices = await _context.SaleInvoices.ToListAsync();
            var orders = await _context.CustomerOrders.ToListAsync();

            // Combine all transactions into one list of (amount, date)
            var allTransactions = sales.Select(s => new { s.Amount, Date = s.SaleDate })
                .Concat(invoices.Select(i => new { Amount = i.TotalAmount, Date = i.CreatedAt }))
                .Concat(orders.Select(o => new { Amount = o.Total, Date = o.CreatedAt }))
                .ToList();

            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);
            var weekStart = today.AddDays(-(int)today.DayOfWeek); // Sunday as week start
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            var breakdown = new TotalSalesBreakdownDto
            {
                Today = allTransactions.Where(t => t.Date.Date == today).Sum(t => t.Amount),
                Yesterday = allTransactions.Where(t => t.Date.Date == yesterday).Sum(t => t.Amount),
                ThisWeek = allTransactions.Where(t => t.Date.Date >= weekStart).Sum(t => t.Amount),
                ThisMonth = allTransactions.Where(t => t.Date.Date >= monthStart).Sum(t => t.Amount),
                ThisYear = allTransactions.Where(t => t.Date.Date >= yearStart).Sum(t => t.Amount),
                AllTime = allTransactions.Sum(t => t.Amount),
                TotalTransactions = allTransactions.Count
            };

            return Json(breakdown);
        }

        // GET: /Dashboard/GetTop5HighestSales
        [HttpGet]
        public async Task<IActionResult> GetTop5HighestSales()
        {
            var allSales = await GetAllSalesMergedAsync();
            var top5 = allSales.OrderByDescending(s => s.Amount).Take(5).ToList();
            return Json(top5);
        }

        // GET: /Dashboard/GetTop5LowestSales
        [HttpGet]
        public async Task<IActionResult> GetTop5LowestSales()
        {
            var allSales = await GetAllSalesMergedAsync();
            var bottom5 = allSales.OrderBy(s => s.Amount).Take(5).ToList();
            return Json(bottom5);
        }

        // GET: /Dashboard/GetSalesByDate?date=2026-07-20
        [HttpGet]
        public async Task<IActionResult> GetSalesByDate(string date)
        {
            if (!DateTime.TryParse(date, out var selectedDate))
                return BadRequest(new { message = "Invalid date format." });

            var allSales = await GetAllSalesMergedAsync();
            var dailySales = allSales
                .Where(s => s.Date.Date == selectedDate.Date)
                .OrderByDescending(s => s.Date)
                .ToList();

            var response = new DailySalesResponseDto
            {
                Date = selectedDate.ToString("yyyy-MM-dd"),
                TotalForDay = dailySales.Sum(s => s.Amount),
                Count = dailySales.Count,
                Sales = dailySales
            };

            return Json(response);
        }

        // Helper: Merges Sales + SaleInvoices + CustomerOrders into one unified list
        private async Task<List<SaleDetailDto>> GetAllSalesMergedAsync()
        {
            var sales = await _context.Sales.ToListAsync();
            var invoices = await _context.SaleInvoices
                .Include(i => i.Items)
                .ToListAsync();
            var orders = await _context.CustomerOrders.ToListAsync();

            var result = new List<SaleDetailDto>();

            // From simple Sales table (no product items)
            foreach (var s in sales)
            {
                result.Add(new SaleDetailDto
                {
                    CustomerName = s.CustomerName ?? "Unknown",
                    Amount = s.Amount,
                    Date = s.SaleDate,
                    Source = "Shop",
                    Items = new List<SaleItemDto>()
                });
            }

            // From SaleInvoices (rich data with product items!)
            foreach (var inv in invoices)
            {
                result.Add(new SaleDetailDto
                {
                    CustomerName = inv.CustomerName,
                    Amount = inv.TotalAmount,
                    Date = inv.CreatedAt,
                    Source = "Invoice",
                    Items = inv.Items.Select(it => new SaleItemDto
                    {
                        Description = it.Description,
                        Quantity = it.Quantity,
                        UnitPrice = it.UnitPrice,
                        TotalPrice = it.TotalPrice
                    }).ToList()
                });
            }

            // From CustomerOrders (online orders — items stored in JSON, not parsed here)
            foreach (var o in orders)
            {
                result.Add(new SaleDetailDto
                {
                    CustomerName = o.FullName,
                    Amount = o.Total,
                    Date = o.CreatedAt,
                    Source = "Online",
                    Items = new List<SaleItemDto>()
                });
            }

            return result;
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