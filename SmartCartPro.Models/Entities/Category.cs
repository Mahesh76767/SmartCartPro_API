
namespace SmartCartPro.Models.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public bool? IsActive { get; set; } = true;
        public int Count { get; set; }

    }
}