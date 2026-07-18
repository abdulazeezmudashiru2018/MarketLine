using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastPurchaseAt { get; set; } = DateTime.Now;

        public List<SaleInvoice> Invoices { get; set; } = new();
    }
}