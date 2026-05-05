using System.ComponentModel.DataAnnotations;
namespace SmartCartPro.Models.DTOs.Order
{
    public class UpdateOrderStatusDto
    {
        [Required] public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class CancelOrderDto
    {
        [Required] public string Reason { get; set; } = string.Empty;
    }

    public class ValidateDiscountDto
    {
        [Required] public string Code { get; set; } = string.Empty;
    }

    public class DiscountResponseDto
    {
        public int CodeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}