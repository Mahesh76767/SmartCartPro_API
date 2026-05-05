using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Product;
using SmartCartPro.Models.Entities;

namespace SmartCartPro.Business.Interfaces
{
    public interface IProductService
    {
        Task<PagedResult<ProductResponseDto>> GetAllAsync(ProductFilterDto filter);
        Task<ProductResponseDto> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateProductDto dto);
        Task<bool> UpdateAsync(int id, UpdateProductDto dto);
        Task<bool> DeleteAsync(int id);
        Task<string> GenerateDescriptionAsync(int productId);
    }
}