using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Customer;

namespace SmartCartPro.DataAccess.Interfaces
{
    public interface ICustomerRepository
    {
        Task<PagedResult<CustomerResponseDto>> GetAllAsync(CustomerFilterDto filter);

        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task<CustomerResponseDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateCustomerDto dto);
        Task<bool> UpdateAsync(int id, UpdateCustomerDto dto);
        Task<bool> SoftDeleteAsync(int id);
        Task<bool> HasActiveOrdersAsync(int customerId);
        Task<List<CustomerOrderDto>> GetOrdersAsync(int id);
        Task<List<ReviewResponseDto>> GetReviewsAsync(int customerId);
        Task<int> AddReviewAsync(int customerId, CreateReviewDto dto, string? sentimentLabel, decimal? sentimentScore);
    }
}
