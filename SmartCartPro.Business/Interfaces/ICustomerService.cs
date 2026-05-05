using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Customer;
using SmartCartPro.Models.Entities;

namespace SmartCartPro.Business.Interfaces
{
    // TODO: Add Customer service method signatures here
    public interface ICustomerService
    {
        Task<PagedResult<CustomerResponseDto>> GetAllAsync(CustomerFilterDto filter);
        Task<CustomerResponseDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateCustomerDto dto);
        Task<bool> UpdateAsync(int id, UpdateCustomerDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<CustomerOrderDto>> GetOrdersAsync(int id);
        Task<List<ReviewResponseDto>> GetReviewsAsync(int customerId);
        Task<ReviewResponseDto> AddReviewAsync(int customerId, CreateReviewDto dto);
    }
}