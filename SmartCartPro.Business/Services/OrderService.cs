using SmartCartPro.Business.Interfaces;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.DataAccess.Repositories;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Order;

namespace SmartCartPro.Business.Services
{
    public class OrderService : IOrderService
    {
        public readonly IOrderRepository _repo;

        public OrderService(IOrderRepository repo)
        {
            _repo = repo;
        }

        public static readonly Dictionary<string, List<string>> _validTransitions = new()
        {
            ["Pending"] = new() { "Confirmed", "Cancelled" },
            ["Confirmed"] = new() { "Processing", "Cancelled" },
            ["Processing"] = new() { "Shipped", "Cancelled" },
            ["Shipped"] = new() { "Delivered" },
            ["Delivered"] = new(),
            ["Cancelled"] = new()
        };

        public async Task<PagedResult<OrderResponseDto>> GetAllAsync(OrderFilterDto filter) =>
           await _repo.GetAllAsync(filter);

        public async Task<OrderResponseDto?> GetByIdAsync(int id) =>
            await _repo.GetByIdAsync(id);

        public async Task<int> CreateAsync(CreateOrderDto dto)
        {
            // Step 1: Validate all products + stock before doing anything
            var productInfos = new List<(int productId, int quantity, decimal price)>();

            foreach(var item in dto.Items)
            {
                var (exists, price, stock, name) = await _repo.GetProductInfoAsync(item.ProductId);
                if (!exists)
                    throw new AppException($"Product ID {item.ProductId} not found or inactive.");
                if (stock < item.Quantity)
                    throw new AppException($"Insufficient stock for '{name}'. Available: {stock}, Requested: {item.Quantity}");

                productInfos.Add((item.ProductId, item.Quantity, price));
            }

            // Step 2: Validate discount code if provided
            int? discountCodeId = null;
            decimal discountPercent = 0;

            if (!string.IsNullOrWhiteSpace(dto.DiscountCode))
            {
                var discount = await _repo.ValidateDiscountAsync(dto.DiscountCode);
                if (discount == null)
                    throw new AppException("Invalid or expired discount code.");

                discountCodeId = discount.CodeId;
                discountPercent = discount.DiscountPercent;
            }

            // Step 3: Calculate totals
            var subTotal = productInfos.Sum(x => x.price * x.quantity);
            var discountAmount = subTotal * (discountPercent / 100);
            var totalAmount = subTotal - discountAmount;

            // Step 4: Create order in transaction
            return await _repo.CreateAsync(dto, totalAmount, discountAmount, discountCodeId);
        }

        public async Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _repo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Order {id} not found.");

            if (!_validTransitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(dto.Status))
                throw new AppException($"Cannot change status from '{order.Status}' to '{dto.Status}'.");

            return await _repo.UpdateStatusAsync(id, dto.Status);
        }

        public async Task<bool> CancelAsync(int id, CancelOrderDto dto)
        {
            var order = await _repo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Order {id} not found.");

            if (order.Status == "Delivered")
                throw new AppException("Cannot cancel a delivered order.");

            if (order.Status == "Cancelled")
                throw new AppException("Order is already cancelled.");

            return await _repo.CancelAsync(id, dto.Reason);
        }

        public async Task<DiscountResponseDto> ValidateDiscountAsync(string code)
        {
            var result = await _repo.ValidateDiscountAsync(code);
            if (result == null) throw new AppException("Invalid or expired discount code.");
            return result;
        }

    }
}