using SmartCartPro.Models.DTOs.Order;
namespace SmartCartPro.Business.Validators
{
    public static class OrderValidator
    {
        public static List<string> Validate(CreateOrderDto dto)
        {
            var errors = new List<string>();
            if (dto.CustomerId <= 0) errors.Add("Valid CustomerId is required.");
            if (string.IsNullOrWhiteSpace(dto.ShippingAddress)) errors.Add("Shipping address is required.");
            if (dto.Items == null || dto.Items.Count == 0) errors.Add("Order must have at least one item.");
            return errors;
        }
    }
}