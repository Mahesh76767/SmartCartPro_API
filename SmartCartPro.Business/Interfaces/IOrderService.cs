using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Order;
namespace SmartCartPro.Business.Interfaces
{
    public interface IOrderService
    {
        Task<PagedResult<OrderResponseDto>> GetAllAsync(OrderFilterDto filter);
        Task<OrderResponseDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateOrderDto dto);
        Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<bool> CancelAsync(int id, CancelOrderDto dto);
        Task<DiscountResponseDto> ValidateDiscountAsync(string code);
    }
}