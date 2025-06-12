using System.Collections.Generic;
using System;

namespace CyberTech.Models
{
    public class ProductDetailViewModel
    {
        public required Product Product { get; set; }
        public required List<Product> RelatedProducts { get; set; }
        public bool IsInWishlist { get; set; } = false;
        public bool IsSubscribedToStock { get; set; } = false;

        public bool CanReview { get; set; } = false;
        public bool HasUserReviewed { get; set; } = false;
        public Review? UserReview { get; set; }

        // Pagination cho reviews
        public List<Review> PaginatedReviews { get; set; } = new List<Review>();
        public int ReviewsCurrentPage { get; set; } = 1;
        public int ReviewsPerPage { get; set; } = 5;
        public int ReviewsTotalPages { get; set; } = 1;
        public int ReviewsTotalCount { get; set; } = 0;
    }

    public class SubSubcategoryViewModel
    {
        public int SubSubcategoryID { get; set; }
        public required string Name { get; set; }
        public required string Url { get; set; }
    }

    public class SubcategoryViewModel
    {
        public int SubcategoryID { get; set; }
        public required string Name { get; set; }
        public string? Group { get; set; }
        public required List<SubSubcategoryViewModel> SubSubcategories { get; set; }
        public required string Url { get; set; }
    }

    public class CategoryViewModel
    {
        public int CategoryID { get; set; }
        public required string Name { get; set; }
        public required string Icon { get; set; }
        public required List<SubcategoryViewModel> Subcategories { get; set; }
        public required string Url { get; set; }
    }

    public class HomeIndexViewModel
    {
        public required List<ProductViewModel> Products { get; set; }
        public required List<CategoryViewModel> Categories { get; set; }
        public required List<ProductViewModel> FlashSaleProducts { get; set; }
        public required List<ProductViewModel> LaptopGamingBestSellers { get; set; }
        public required List<ProductViewModel> LaptopOfficeBestSellers { get; set; }
        public required List<ProductViewModel> PcGamingBestSellers { get; set; }
        public required List<ProductViewModel> MouseBestSellers { get; set; }
        public required List<ProductViewModel> MonitorBestSellers { get; set; }
        public required List<ProductViewModel> KeyboardBestSellers { get; set; }
    }

    public class ProductViewModel
    {
        public int ProductID { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal? SalePercentage { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public required string PrimaryImageUrl { get; set; }
        public required string PrimaryImageUrlSmall { get; set; }
        public required string Url { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public required Dictionary<string, string> Attributes { get; set; }
        public bool IsInStock { get; set; }
        public required string Brand { get; set; }
        public required SubSubcategory SubSubcategory { get; set; }
        public required string Status { get; set; }

        // Computed properties
        public decimal CurrentPrice
        {
            get
            {
                // Nếu có SalePrice trực tiếp, dùng SalePrice
                if (SalePrice.HasValue && SalePrice > 0)
                    return SalePrice.Value;

                // Nếu có SalePercentage, tính từ percentage với Round Down
                if (SalePercentage.HasValue && SalePercentage > 0)
                {
                    var calculatedPrice = Price * (1 - SalePercentage.Value / 100);
                    // Round xuống nghìn gần nhất để có UX tốt hơn
                    return Math.Floor(calculatedPrice / 1000) * 1000;
                }

                // Không có sale, trả về giá gốc
                return Price;
            }
        }

        public bool HasDiscount
        {
            get
            {
                // Có SalePrice và nhỏ hơn giá gốc
                if (SalePrice.HasValue && SalePrice > 0 && SalePrice < Price)
                    return true;

                // Có SalePercentage lớn hơn 0
                if (SalePercentage.HasValue && SalePercentage > 0)
                    return true;

                return false;
            }
        }

        public decimal DiscountAmount => HasDiscount ? (Price - CurrentPrice) : 0;
        public string ImageUrl => PrimaryImageUrl;
    }
}