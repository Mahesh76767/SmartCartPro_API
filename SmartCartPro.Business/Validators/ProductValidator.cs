using SmartCartPro.Models.DTOs.Product;
namespace SmartCartPro.Business.Validators
{
    public static class ProductValidator
    {
        public static List<string> Validate(CreateProductDto dto)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.SKU)) errors.Add("SKU is required.");
            if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Product name is required.");
            if (dto.Price <= 0) errors.Add("Price must be greater than 0.");
            if (dto.CategoryId <= 0) errors.Add("Valid CategoryId is required.");
            return errors;
        }
    }
}