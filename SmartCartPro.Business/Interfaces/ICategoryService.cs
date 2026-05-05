using SmartCartPro.Models.Entities;

namespace SmartCartPro.Business.Interfaces
{

    public interface ICategoryService
    {
        Task<List<Category>> GetCategories();
        Task<Category?> GetCategoryById(int id);
        Task<int> CreateAsync(Category id);
        Task<bool> UpdateAsync(int id, Category dto);
        Task<bool> DeleteAsync(int id);
    }
}