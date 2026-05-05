using SmartCartPro.Models.Common;
namespace SmartCartPro.Models.DTOs.Product
{
    public class ProductFilterDto : PaginationParams
    {
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStock { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}