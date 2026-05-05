using SmartCartPro.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCartPro.DataAccess.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category?>> GetCategories();
        Task<Category?> GetCategoriesById(int Id);
        Task<int> CreateAsync(Category category);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
        Task<bool> UpdateAsync(Category category);
        Task<bool> HasProductsAsync(int categoryId);
        Task<bool> SoftDeleteAsync(int id);



    }
}
