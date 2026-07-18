using Microsoft.AspNetCore.Http;

namespace MarketLine.Models
{
    // Used to bind the multipart/form-data posted from the Add/Edit modal
    public class ProductFormInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public IFormFile? Image { get; set; }
        public string? Barcode { get; set; }
    }

    // Shape returned to the front-end after create/edit, so the JS can
    // render/update a card without a full page reload
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }
        public string? Barcode { get; set; }

        public static ProductDto FromEntity(Product p) => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            ImagePath = p.ImagePath,
             Barcode = p.Barcode
        };
    }


}
