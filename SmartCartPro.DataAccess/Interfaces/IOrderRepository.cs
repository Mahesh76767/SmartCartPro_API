using SmartCartPro.DataAccess.Repositories;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Order;
using SmartCartPro.Models.Entities;
namespace SmartCartPro.DataAccess.Interfaces
{
    public interface IOrderRepository
    {
        Task<PagedResult<OrderResponseDto>> GetAllAsync(OrderFilterDto filter);
        Task<OrderResponseDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateOrderDto dto, decimal totalAmount, decimal discountAmount, int? discountCodeId);
        Task<DiscountResponseDto?> ValidateDiscountAsync(string code);
        Task<(bool exists, decimal price, int stock, string name)> GetProductInfoAsync(int productId);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> CancelAsync(int id, string reason);

    }
}