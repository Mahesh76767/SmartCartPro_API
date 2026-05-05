using SmartCartPro.Models.Entities;
using SmartCartPro.Models.DTOs.Product;
using SmartCartPro.Models.Common;
namespace SmartCartPro.DataAccess.Interfaces
{
    public interface IProductRepository
    {
        Task<PagedResult<ProductResponseDto>> GetAllAsync(ProductFilterDto filter);
        Task<ProductResponseDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateProductDto dto);
        Task<bool> SKUExistsAsync(string sku, int? excludeId = null);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task<bool> UpdateAsync(int id, UpdateProductDto dto);
        Task<bool> SoftDeleteAsync(int id);
    }
}