using SmartCartPro.Business.Interfaces;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Customer;
using SmartCartPro.Models.Entities;

namespace SmartCartPro.Business.Services
{
    // TODO: Implement CustomerService
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;
        private readonly IAIService _ai;

        public CustomerService(ICustomerRepository repo, IAIService ai)
        {
            _repo = repo;
            _ai = ai;
        }

        public Task<PagedResult<CustomerResponseDto>> GetAllAsync(CustomerFilterDto filter) =>
            _repo.GetAllAsync(filter);

        public Task<CustomerResponseDto?> GetByIdAsync(int id) =>
           _repo.GetByIdAsync(id);

        public async Task<int> CreateAsync(CreateCustomerDto dto)
        {
            if (await _repo.EmailExistsAsync(dto.Email))
                throw new AppException($"Email '{dto.Email}' is already registered.");
            return await _repo.CreateAsync(dto);
        }

        public async Task<bool>UpdateAsync(int id, UpdateCustomerDto dto)
        {
            var existing = await _repo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Customer {id} not found.");

            if (dto.Email != null && dto.Email.ToLower() != existing.Email.ToLower())
                if (await _repo.EmailExistsAsync(dto.Email, id))
                    throw new AppException("Email is already in use by another customer.");


            return await _repo.UpdateAsync(id, dto);
        }

        public async Task<bool>DeleteAsync(int id)
        {
            if (await _repo.HasActiveOrdersAsync(id))
                throw new AppException("Cannot delete customer with active orders.");
            var deleted = await _repo.SoftDeleteAsync(id);

            if (!deleted) throw new NotFoundException($"Customer {id} not found.");
            return true;
        }

        public Task<List<CustomerOrderDto>> GetOrdersAsync(int customerId) =>
           _repo.GetOrdersAsync(customerId);

        public Task<List<ReviewResponseDto>> GetReviewsAsync(int customerId) =>
            _repo.GetReviewsAsync(customerId);

        public async Task<ReviewResponseDto> AddReviewAsync(int customerId, CreateReviewDto dto)
        {
            var customer = await _repo.GetByIdAsync(customerId)
                ?? throw new NotFoundException($"Customer {customerId} not found.");

            string? sentimentLabel = null;
            decimal? sentimentScore = null;

            if (!string.IsNullOrWhiteSpace(dto.Comment))
            {
                try
                {
                    var sentiment = await _ai.AnalyzeSentimentAsync(dto.Comment);
                    sentimentLabel = sentiment.Label;
                    sentimentScore = sentiment.Score;
                }
                catch
                {
                    // AI failure should not block saving the review
                }
            }

            var reviewId = await _repo.AddReviewAsync(customerId, dto, sentimentLabel, sentimentScore);
            return new ReviewResponseDto
            {
                ReviewId = reviewId,
                ProductId = dto.ProductId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                SentimentLabel = sentimentLabel,
                SentimentScore = sentimentScore,
                CreatedAt = DateTime.UtcNow
            };
        }

    }
}