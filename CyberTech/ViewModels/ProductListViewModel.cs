using CyberTech.Models;

namespace CyberTech.ViewModels
{
    public class ProductListViewModel
    {
        public IEnumerable<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        public ProductFilterModel Filter { get; set; } = new ProductFilterModel();
        public int TotalProducts { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public string CategoryName { get; set; }
        public string BreadcrumbPath { get; set; }
        
        // Dữ liệu cho bộ lọc
        public IEnumerable<CategoryAttributeFilter> CategoryAttributes { get; set; } = new List<CategoryAttributeFilter>();
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
    }

    public class ProductFilterModel
    {
        public string? CategoryId { get; set; }
        public string? SubcategoryId { get; set; }
        public string? SubSubcategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Status { get; set; }
        public bool? HasDiscount { get; set; }
        public bool? InStock { get; set; }
        public string? SortBy { get; set; } = "name";
        public string? SortOrder { get; set; } = "asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 28;
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public string? SearchQuery { get; set; }
        public string? RefineQuery { get; set; }
        public List<string>? Categories { get; set; }
        public List<string>? Brands { get; set; }
    }

    public class CategoryAttributeFilter
    {
        public string AttributeName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public IEnumerable<AttributeValueOption> Values { get; set; } = new List<AttributeValueOption>();
    }

    public class AttributeValueOption
    {
        public string Value { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public bool IsSelected { get; set; }
    }

    // Giữ lại để tương thích với code cũ
    public class FilterAttribute
    {
        public string Name { get; set; }
        public IEnumerable<string> Values { get; set; } = new List<string>();
    }
} 