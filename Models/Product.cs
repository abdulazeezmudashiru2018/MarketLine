using System;
using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class Product
    {
        public int Id { get; set; }
       
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string? ImagePath { get; set; }

        
        [StringLength(64)]
        public string? Barcode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
