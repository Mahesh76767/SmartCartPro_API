using System.ComponentModel.DataAnnotations;

namespace SmartCartPro.Models.Entities
{
    public class Product
    {
        [Required(ErrorMessage = "SKU is required")]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public string? ImageUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int InitialStock { get; set; } = 0;

        [Range(1, int.MaxValue)]
        public int MinStockLevel { get; set; } = 5;

        public int MaxStockLevel { get; set; } = 500;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}