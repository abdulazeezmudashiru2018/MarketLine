using System;
using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? CustomerName { get; set; }

        // Optional path/url to a customer avatar image. If empty, the view
        // will render the customer's initials instead.
        public string? AvatarUrl { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.Now;
    }
}
