using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using SmartCartPro.Business.Interfaces;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.Entities;
using System;
using System.Threading.Tasks;

namespace SmartCartPro.Business.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly IConfiguration _configuration;

        public CategoryService(ICategoryRepository repo, IConfiguration configuration)
        {
            _repo = repo;
            _configuration = configuration;
        }

        public async Task<List<Category>> GetCategories()
        {
            var result = await _repo.GetCategories();

            if(result == null || result.Count == 0)
            {
                throw new AppException("No categories found", 404);
            }

            return result;
        }

        public async Task<Category?> GetCategoryById(int id)
        {
            var result = await _repo.GetCategoriesById(id);

            if (result == null)
            {
                throw new AppException("Category not found", 404);
            }

            return result;
        }


        public async Task<int> CreateAsync(Category dto)
        {
            if (await _repo.NameExistsAsync(dto.Name))
                throw new AppException($"Category '{dto.Name}' already exists.");

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                ParentCategoryId = dto.ParentCategoryId,
                IsActive = true
            };

            return await _repo.CreateAsync(category);
        }


        public async Task<bool> UpdateAsync (int id, Category dto)
        {
            var existing = await _repo.GetCategoriesById(id)
                ?? throw new NotFoundException($"Category with ID {id} not found.");

            if (dto.Name != null && dto.Name != existing.Name)
                if (await _repo.NameExistsAsync(dto.Name, id))
                    throw new AppException($"Category name '{dto.Name}' already exists.");

            var category = new Category
            {
                CategoryId = id,
                Name = dto.Name?.Trim() ?? existing.Name,
                Description = dto.Description?.Trim() ?? existing.Description,
                ParentCategoryId = dto.ParentCategoryId ?? existing.ParentCategoryId,
                IsActive = dto.IsActive ?? existing.IsActive
            };

            return await _repo.UpdateAsync(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                bool hasProducts = await _repo.HasProductsAsync(id);
                if (hasProducts)
                {
                    throw new AppException("Cannot delete category — it has active products.Move products first.");
                }

                var exits = await _repo.GetCategoriesById(id)
                    ?? throw new NotFoundException($"Category with ID {id} not found.");

                return await _repo.SoftDeleteAsync(id);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occurred: {ex.Message}");
                throw;
            }
        }


    }
}