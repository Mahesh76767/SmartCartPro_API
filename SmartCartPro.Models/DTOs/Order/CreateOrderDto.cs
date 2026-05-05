using System.ComponentModel.DataAnnotations;
namespace SmartCartPro.Models.DTOs.Order
{
    public class CreateOrderDto
    {
        [Required] public int CustomerId { get; set; }
        [Required] public string ShippingAddress { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? DiscountCode { get; set; }

        [Required, MinLength(1, ErrorMessage = "Order must have at least 1 item")]
        public List<OrderItemInputDto> Items { get; set; } = new();
    }

    public class OrderItemInputDto
    {
        [Required] public int ProductId { get; set; }
        [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }

}