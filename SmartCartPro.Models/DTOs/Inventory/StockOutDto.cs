using System.ComponentModel.DataAnnotations;
namespace SmartCartPro.Models.DTOs.Inventory
{
    public class StockOutDto
    {
        [Required] public int ProductId { get; set; }
        [Required, Range(1, int.MaxValue)] public int Quantity { get; set; }
        [Required] public string Reason { get; set; } = string.Empty;
    }
}