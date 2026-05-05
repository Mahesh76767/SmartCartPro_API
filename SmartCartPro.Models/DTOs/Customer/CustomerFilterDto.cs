using SmartCartPro.Models.Common;
using System;

namespace SmartCartPro.Models.DTOs.Customer
{
    public class CustomerFilterDto : PaginationParams
    {
        public string? SortBy { get; set; } = "createdAt";

    }
}