using System.ComponentModel.DataAnnotations;
namespace SmartCartPro.Models.DTOs.Product
{
    public class CreateProductDto
    {
        [Required] public string SKU { get; set; } = string.Empty;
        [Required] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required, Range(0.01, double.MaxValue)] public decimal Price { get; set; }
        [Required] public int CategoryId { get; set; }
        public string? ImageUrl { get; set; }
        public int InitialStock { get; set; } = 0;
        public int MinStockLevel { get; set; } = 5;
        public int MaxStockLevel { get; set; } = 1000;
    }
}