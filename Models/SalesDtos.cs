using System;
using System.Collections.Generic;

namespace MarketLine.Models
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }

    public class ReceiptItemDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class ReceiptSummaryDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ReceiptImagePath { get; set; }
        public List<ReceiptItemDto> Items { get; set; } = new();
    }

    public class CustomerReceiptsResponse
    {
        public string CustomerName { get; set; } = string.Empty;
        public List<ReceiptSummaryDto> Receipts { get; set; } = new();
    }

    // Posted from the Sales invoice page when the user completes a sale
    public class SaveInvoiceRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }
        public List<SaveInvoiceItemRequest> Items { get; set; } = new();

        // Data URL (e.g. "data:image/png;base64,...") captured from the
        // Preview page's rendered receipt, saved as a PNG on disk.
        public string? ReceiptImageBase64 { get; set; }
    }

    public class SaveInvoiceItemRequest
    {
        public int? ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // ============================================================
    // NEW: DTOs for Dashboard Popup Buttons
    // (Total Sales, Highest, Lowest, Daily Sales)
    // ============================================================

    public class TotalSalesBreakdownDto
    {
        public decimal Today { get; set; }
        public decimal Yesterday { get; set; }
        public decimal ThisWeek { get; set; }
        public decimal ThisMonth { get; set; }
        public decimal ThisYear { get; set; }
        public decimal AllTime { get; set; }
        public int TotalTransactions { get; set; }
    }

    public class SaleDetailDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Source { get; set; } = string.Empty; // "Shop", "Invoice", "Online"
        public List<SaleItemDto> Items { get; set; } = new();
    }

    public class SaleItemDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class DailySalesResponseDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal TotalForDay { get; set; }
        public int Count { get; set; }
        public List<SaleDetailDto> Sales { get; set; } = new();
    }
}