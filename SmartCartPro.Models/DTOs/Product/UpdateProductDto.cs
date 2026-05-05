namespace SmartCartPro.Models.DTOs.Product
{
    public class UpdateProductDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? CategoryId { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsActive { get; set; }
        public int? MinStockLevel { get; set; }
        public int? MaxStockLevel { get; set; }
    }
}