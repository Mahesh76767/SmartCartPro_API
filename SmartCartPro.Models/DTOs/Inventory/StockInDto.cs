using System.ComponentModel.DataAnnotations;
namespace SmartCartPro.Models.DTOs.Inventory
{
    public class StockInDto
    {
        [Required] public int ProductId { get; set; }
        [Required, Range(1, int.MaxValue)] public int Quantity { get; set; }
        public int? SupplierId { get; set; }
        public string? Reason { get; set; }
    }
}