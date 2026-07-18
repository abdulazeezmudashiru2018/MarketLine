using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class CustomerOrder
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string ShippingAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string CartItemsJson { get; set; } = "[]";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}