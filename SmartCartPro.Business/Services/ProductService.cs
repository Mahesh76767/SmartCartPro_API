using SmartCartPro.Business.Interfaces;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.AI;
using SmartCartPro.Models.DTOs.Product;
using SmartCartPro.Models.Entities;

namespace SmartCartPro.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IAIService _aiService;

        public ProductService(
            IProductRepository productRepo,
            ICategoryRepository categoryRepo,
            IAIService aiService)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _aiService = aiService;
        }

        public async Task<PagedResult<ProductResponseDto>> GetAllAsync(ProductFilterDto filter)
        {
            return await _productRepo.GetAllAsync(filter);
        }

        public async Task<ProductResponseDto> GetByIdAsync(int id)
        {
            return await _productRepo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Product with ID {id} not found.");
        }


        public async Task<int> CreateAsync(CreateProductDto dto)
        {
            if (await _productRepo.SKUExistsAsync(dto.SKU))
                throw new AppException($"SKU '{dto.SKU}' already exists. Use a different SKU.");

            if (!await _productRepo.CategoryExistsAsync(dto.CategoryId))
                throw new AppException($"Category ID {dto.CategoryId} does not exist.");

            return await _productRepo.CreateAsync(dto); 
        }


        public async Task<bool> UpdateAsync(int id, UpdateProductDto dto)
        {
            var existing = await _productRepo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Product with ID {id} not found.");

            if (dto.CategoryId.HasValue && !await _productRepo.CategoryExistsAsync(dto.CategoryId.Value))
                throw new AppException($"Category ID {dto.CategoryId} does not exist.");

            return await _productRepo.UpdateAsync(id, dto);
        }

        public async Task<bool>DeleteAsync(int id)
        {
            var existing = await _productRepo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Product with ID {id} not found.");

            return await _productRepo.SoftDeleteAsync(id);
        }


        public async Task<string> GenerateDescriptionAsync(int productId)
        {
            var product = await _productRepo.GetByIdAsync(productId)
                ?? throw new NotFoundException($"Product with ID {productId} not found.");

            var dto = new GenerateDescriptionDto
            {
                ProductName = product.Name,
                Category = product.CategoryName,
                KeyFeatures = null
            };

            return await _aiService.GenerateProductDescriptionAsync(dto);
        }

    }
}