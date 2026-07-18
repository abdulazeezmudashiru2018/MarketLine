using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;
using MarketLine.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MarketLine.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Customers -> the customer record page
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(customers);
        }

        // GET: /Customers/Receipts/5  -> JSON list of every sale ever made
        // to this customer, shown when their name is clicked.
        [HttpGet]
        public async Task<IActionResult> Receipts(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(new { message = "Customer not found." });

            var invoices = await _context.SaleInvoices
                .Where(s => s.CustomerId == id)
                .Include(s => s.Items)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var response = new CustomerReceiptsResponse
            {
                CustomerName = customer.Name,
                Receipts = invoices.Select(inv => new ReceiptSummaryDto
                {
                    Id = inv.Id,
                    CreatedAt = inv.CreatedAt,
                    TotalAmount = inv.TotalAmount,
                    ReceiptImagePath = inv.ReceiptImagePath,
                    Items = inv.Items.Select(i => new ReceiptItemDto
                    {
                        Description = i.Description,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.TotalPrice
                    }).ToList()
                }).ToList()
            };

            return Ok(response);
        }

        // GET: /Customers/Search?q=jo  -> used by the Customer Name
        // autocomplete on the Sales invoice page.
        [HttpGet]
        public async Task<IActionResult> Search(string? q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Ok(new CustomerDto[0]);

            var term = q.Trim();

            var results = await _context.Customers
                .Where(c => EF.Functions.Like(c.Name, $"%{term}%"))
                .OrderByDescending(c => c.LastPurchaseAt)
                .Take(10)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Phone = c.Phone
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: /Customers/SearchByPhone?q=0812  -> used by the Customer
        // Phone Number autocomplete on the Sales invoice page. Phone is
        // the more reliable unique key since two customers can share a name.
        [HttpGet]
        public async Task<IActionResult> SearchByPhone(string? q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Ok(new CustomerDto[0]);

            var term = q.Trim();

            var results = await _context.Customers
                .Where(c => c.Phone != null && EF.Functions.Like(c.Phone, $"%{term}%"))
                .OrderByDescending(c => c.LastPurchaseAt)
                .Take(10)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Phone = c.Phone
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}