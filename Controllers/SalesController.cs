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
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SalesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Sales/Create -> the "Add Sales" invoice page
        public IActionResult Create()
        {
            return View();
        }

        // GET: /Sales/Preview -> the receipt preview page. The actual
        // invoice data is handed over from Create.cshtml via
        // sessionStorage (nothing is saved to the DB until this page's
        // own Save button is used), so this action just renders the shell.
        public IActionResult Preview()
        {
            return View();
        }

        // POST: /Sales/Save  (JSON body: SaveInvoiceRequest)
        // Upserts the customer record (matched by name only, so a given
        // customer never ends up with more than one record) and stores
        // the invoice + line items + a snapshot of the receipt image.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] SaveInvoiceRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CustomerName))
                return BadRequest(new { message = "Customer name is required." });

            if (request.Items == null || !request.Items.Any())
                return BadRequest(new { message = "Add at least one item before saving." });

            var name = request.CustomerName.Trim();
            var phone = request.CustomerPhone?.Trim();

            // Match strictly by name (case-insensitive) so a customer is
            // never duplicated even if phone/address differ between visits.
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

            if (customer == null)
            {
                customer = new Customer
                {
                    Name = name,
                    Address = request.CustomerAddress?.Trim(),
                    Phone = phone,
                    CreatedAt = DateTime.Now,
                    LastPurchaseAt = DateTime.Now
                };
                _context.Customers.Add(customer);
            }
            else
            {
                // Keep the record fresh with the latest details supplied
                if (!string.IsNullOrWhiteSpace(request.CustomerAddress))
                    customer.Address = request.CustomerAddress.Trim();
                if (!string.IsNullOrWhiteSpace(phone))
                    customer.Phone = phone;
                customer.LastPurchaseAt = DateTime.Now;
            }

            var invoice = new SaleInvoice
            {
                Customer = customer,
                CustomerName = name,
                CustomerAddress = request.CustomerAddress?.Trim(),
                CustomerPhone = phone,
                CreatedAt = DateTime.Now
            };

            decimal grandTotal = 0m;

            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Description) || item.Quantity <= 0)
                    continue;

                var lineTotal = item.Quantity * item.UnitPrice;
                grandTotal += lineTotal;

                invoice.Items.Add(new SaleInvoiceItem
                {
                    ProductId = item.ProductId,
                    Description = item.Description.Trim(),
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = lineTotal
                });
            }

            if (!invoice.Items.Any())
                return BadRequest(new { message = "Add at least one valid item before saving." });

            invoice.TotalAmount = grandTotal;

            // Save the receipt image (captured client-side via html2canvas
            // on the Preview page) as a PNG under wwwroot/uploads/receipts.
            if (!string.IsNullOrWhiteSpace(request.ReceiptImageBase64))
            {
                var savedPath = TrySaveReceiptImage(request.ReceiptImageBase64);
                if (savedPath != null)
                    invoice.ReceiptImagePath = savedPath;
            }

            _context.SaleInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            return Ok(new { invoiceId = invoice.Id, total = invoice.TotalAmount, receiptImagePath = invoice.ReceiptImagePath });
        }

        private string? TrySaveReceiptImage(string dataUrl)
        {
            try
            {
                var commaIndex = dataUrl.IndexOf(',');
                if (commaIndex < 0) return null;

                var base64 = dataUrl.Substring(commaIndex + 1);
                var bytes = Convert.FromBase64String(base64);

                var folder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid():N}.png";
                var fullPath = Path.Combine(folder, fileName);
                System.IO.File.WriteAllBytes(fullPath, bytes);

                return $"/uploads/receipts/{fileName}";
            }
            catch
            {
                // Image saving is a bonus, not critical — the invoice data
                // itself is already what matters, so fail quietly.
                return null;
            }
        }
    }
}