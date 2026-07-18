using System;
using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class LandingMedia
    {
        public int Id { get; set; }

        // "left" or "right" — which hero circle this belongs to
        [Required]
        [StringLength(10)]
        public string Slot { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        // "image" or "video"
        [Required]
        [StringLength(10)]
        public string MediaType { get; set; } = "image";

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}