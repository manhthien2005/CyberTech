using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using CyberTech.Models;

namespace CyberTech.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(ApplicationDbContext context, ILogger<RecommendationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ProductViewModel>> GetRecommendationsBasedOnRecentlyViewed(List<int> recentlyViewedIds, int limit = 8)
        {
            try
            {
                if (!recentlyViewedIds.Any())
                {
                    return await GetPopularProducts(limit);
                }

                // 1. Lấy thông tin sản phẩm đã xem
                var recentlyViewedProducts = await _context.Products
                    .Include(p => p.SubSubcategory)
                        .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                    .Where(p => recentlyViewedIds.Contains(p.ProductID))
                    .ToListAsync();

                var recommendations = new List<Product>();

                // 2. Gợi ý sản phẩm tương tự (50% recommendations)
                var similarLimit = limit / 2;
                var similarProducts = await GetSimilarProductsInternal(recentlyViewedProducts, similarLimit);
                recommendations.AddRange(similarProducts);

                // 3. Gợi ý cross-category (30% recommendations)
                var crossLimit = (int)(limit * 0.3);
                var crossProducts = await GetCrossCategoryInternal(recentlyViewedProducts, crossLimit);
                recommendations.AddRange(crossProducts);

                // 4. Gợi ý trending/popular (20% recommendations)
                var trendingLimit = limit - recommendations.Count;
                if (trendingLimit > 0)
                {
                    var trendingProducts = await GetTrendingProducts(trendingLimit, recentlyViewedIds);
                    recommendations.AddRange(trendingProducts);
                }

                // 5. Loại bỏ duplicate và sản phẩm đã xem
                var uniqueRecommendations = recommendations
                    .Where(p => !recentlyViewedIds.Contains(p.ProductID))
                    .GroupBy(p => p.ProductID)
                    .Select(g => g.First())
                    .Take(limit)
                    .ToList();

                return ConvertToViewModels(uniqueRecommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations based on recently viewed");
                return await GetPopularProducts(limit);
            }
        }

        public async Task<List<ProductViewModel>> GetRecommendationsForAbandonedCart(List<int> cartProductIds, int limit = 8)
        {
            try
            {
                if (!cartProductIds.Any())
                {
                    return new List<ProductViewModel>();
                }

                var cartProducts = await _context.Products
                    .Include(p => p.SubSubcategory)
                        .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                    .Where(p => cartProductIds.Contains(p.ProductID))
                    .ToListAsync();

                var recommendations = new List<Product>();

                // 1. Sản phẩm đi kèm/bổ sung (60% recommendations)
                var companionLimit = (int)(limit * 0.6);
                var companionProducts = await GetCompanionProducts(cartProducts, companionLimit);
                recommendations.AddRange(companionProducts);

                // 2. Sản phẩm alternative tương tự nhưng rẻ hơn (25% recommendations)
                var alternativeLimit = (int)(limit * 0.25);
                var alternativeProducts = await GetAlternativeProducts(cartProducts, alternativeLimit);
                recommendations.AddRange(alternativeProducts);

                // 3. Sản phẩm upgrade cao cấp hơn (15% recommendations)
                var upgradeLimit = limit - recommendations.Count;
                if (upgradeLimit > 0)
                {
                    var upgradeProducts = await GetUpgradeProducts(cartProducts, upgradeLimit);
                    recommendations.AddRange(upgradeProducts);
                }

                // Loại bỏ duplicate và sản phẩm đã có trong cart
                var uniqueRecommendations = recommendations
                    .Where(p => !cartProductIds.Contains(p.ProductID))
                    .GroupBy(p => p.ProductID)
                    .Select(g => g.First())
                    .Take(limit)
                    .ToList();

                return ConvertToViewModels(uniqueRecommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting abandoned cart recommendations");
                return new List<ProductViewModel>();
            }
        }

        public async Task<List<ProductViewModel>> GetCrossCategoryRecommendations(List<int> baseProductIds, int limit = 8)
        {
            try
            {
                var baseProducts = await _context.Products
                    .Include(p => p.SubSubcategory)
                        .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                    .Where(p => baseProductIds.Contains(p.ProductID))
                    .ToListAsync();

                var crossProducts = await GetCrossCategoryInternal(baseProducts, limit);
                return ConvertToViewModels(crossProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cross-category recommendations");
                return new List<ProductViewModel>();
            }
        }

        public async Task<List<ProductViewModel>> GetSimilarProducts(List<int> productIds, int limit = 8)
        {
            try
            {
                var baseProducts = await _context.Products
                    .Include(p => p.SubSubcategory)
                        .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                    .Where(p => productIds.Contains(p.ProductID))
                    .ToListAsync();

                var similarProducts = await GetSimilarProductsInternal(baseProducts, limit);
                return ConvertToViewModels(similarProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar products");
                return new List<ProductViewModel>();
            }
        }

        #region Private Helper Methods

        private async Task<List<Product>> GetSimilarProductsInternal(List<Product> baseProducts, int limit)
        {
            var recommendations = new List<Product>();

            foreach (var product in baseProducts)
            {
                // Tìm sản phẩm cùng category, brand tương tự, price range ±30%
                var minPrice = product.GetEffectivePrice() * 0.7m;
                var maxPrice = product.GetEffectivePrice() * 1.3m;

                var similar = await _context.Products
                    .Include(p => p.SubSubcategory)
                        .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .Where(p => p.ProductID != product.ProductID &&
                               p.Status == "Active" &&
                               p.Stock > 0 &&
                               p.SubSubcategory.SubcategoryID == product.SubSubcategory.SubcategoryID &&
                               (p.SalePrice ?? p.Price) >= minPrice &&
                               (p.SalePrice ?? p.Price) <= maxPrice)
                    .OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0)
                    .ThenByDescending(p => p.OrderItems.Sum(oi => oi.Quantity))
                    .Take(3)
                    .ToListAsync();

                recommendations.AddRange(similar);
            }

            return recommendations.Take(limit).ToList();
        }

        private async Task<List<Product>> GetCrossCategoryInternal(List<Product> baseProducts, int limit)
        {
            var recommendations = new List<Product>();

            // Cross-category rules cho Tech products
            var categoryMappings = new Dictionary<string, List<string>>
            {
                ["Laptop Gaming"] = new List<string> { "Chuột", "Bàn phím", "Tai nghe", "Túi laptop" },
                ["Laptop Văn phòng"] = new List<string> { "Chuột", "Bàn phím", "Túi laptop", "Dock sạc" },
                ["PC Gaming"] = new List<string> { "Màn hình", "Chuột", "Bàn phím", "Tai nghe", "Loa" },
                ["PC Văn phòng"] = new List<string> { "Màn hình", "Chuột", "Bàn phím", "Loa", "Webcam" },
                ["Chuột"] = new List<string> { "Bàn phím", "Mousepad", "Laptop Gaming" },
                ["Bàn phím"] = new List<string> { "Chuột", "Mousepad", "Laptop Gaming" },
                ["Màn hình"] = new List<string> { "PC Gaming", "Loa", "Webcam", "Giá đỡ màn hình" }
            };

            foreach (var product in baseProducts)
            {
                var categoryName = product.SubSubcategory?.Name ?? "";
                
                if (categoryMappings.ContainsKey(categoryName))
                {
                    var targetCategories = categoryMappings[categoryName];
                    
                    var crossProducts = await _context.Products
                        .Include(p => p.SubSubcategory)
                            .ThenInclude(ss => ss.Subcategory)
                            .ThenInclude(s => s.Category)
                        .Include(p => p.ProductImages)
                        .Include(p => p.Reviews)
                        .Where(p => p.Status == "Active" &&
                                   p.Stock > 0 &&
                                   targetCategories.Contains(p.SubSubcategory.Name))
                        .OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0)
                        .ThenByDescending(p => p.OrderItems.Sum(oi => oi.Quantity))
                        .Take(2)
                        .ToListAsync();

                    recommendations.AddRange(crossProducts);
                }
            }

            return recommendations.Take(limit).ToList();
        }

        private async Task<List<Product>> GetCompanionProducts(List<Product> cartProducts, int limit)
        {
            // Logic tương tự GetCrossCategoryInternal nhưng focus vào sản phẩm đi kèm
            return await GetCrossCategoryInternal(cartProducts, limit);
        }

        private async Task<List<Product>> GetAlternativeProducts(List<Product> cartProducts, int limit)
        {
            var recommendations = new List<Product>();

            foreach (var product in cartProducts)
            {
                // Tìm sản phẩm cùng category nhưng rẻ hơn 10-30%
                var maxPrice = product.GetEffectivePrice() * 0.9m;

                var alternatives = await _context.Products
                    .Include(p => p.SubSubcategory)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .Where(p => p.ProductID != product.ProductID &&
                               p.Status == "Active" &&
                               p.Stock > 0 &&
                               p.SubSubcategory.SubcategoryID == product.SubSubcategory.SubcategoryID &&
                               (p.SalePrice ?? p.Price) <= maxPrice)
                    .OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0)
                    .Take(2)
                    .ToListAsync();

                recommendations.AddRange(alternatives);
            }

            return recommendations.Take(limit).ToList();
        }

        private async Task<List<Product>> GetUpgradeProducts(List<Product> cartProducts, int limit)
        {
            var recommendations = new List<Product>();

            foreach (var product in cartProducts)
            {
                // Tìm sản phẩm cùng category nhưng đắt hơn 20-50%
                var minPrice = product.GetEffectivePrice() * 1.2m;
                var maxPrice = product.GetEffectivePrice() * 1.5m;

                var upgrades = await _context.Products
                    .Include(p => p.SubSubcategory)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .Where(p => p.ProductID != product.ProductID &&
                               p.Status == "Active" &&
                               p.Stock > 0 &&
                               p.SubSubcategory.SubcategoryID == product.SubSubcategory.SubcategoryID &&
                               (p.SalePrice ?? p.Price) >= minPrice &&
                               (p.SalePrice ?? p.Price) <= maxPrice)
                    .OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0)
                    .Take(1)
                    .ToListAsync();

                recommendations.AddRange(upgrades);
            }

            return recommendations.Take(limit).ToList();
        }

        private async Task<List<Product>> GetTrendingProducts(int limit, List<int> excludeIds)
        {
            return await _context.Products
                .Include(p => p.SubSubcategory)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Include(p => p.OrderItems)
                .Where(p => p.Status == "Active" &&
                           p.Stock > 0 &&
                           !excludeIds.Contains(p.ProductID))
                .OrderByDescending(p => p.OrderItems.Where(oi => oi.Order.CreatedAt >= DateTime.Now.AddDays(-30))
                                                    .Sum(oi => oi.Quantity))
                .ThenByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0)
                .Take(limit)
                .ToListAsync();
        }

        private async Task<List<ProductViewModel>> GetPopularProducts(int limit)
        {
            var products = await _context.Products
                .Include(p => p.SubSubcategory)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Include(p => p.OrderItems)
                .Where(p => p.Status == "Active" && p.Stock > 0)
                .OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity))
                .ThenByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0)
                .Take(limit)
                .ToListAsync();

            return ConvertToViewModels(products);
        }

        private List<ProductViewModel> ConvertToViewModels(List<Product> products)
        {
            return products.Select(p => new ProductViewModel
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Price = p.Price,
                SalePrice = p.SalePrice,
                SalePercentage = p.SalePercentage,
                DiscountedPrice = p.SalePrice,
                PrimaryImageUrl = p.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL ??
                                p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/no-image.png",
                PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL ??
                                     p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/no-image.png",
                Url = $"/Product/ProductDetail/{p.ProductID}",
                AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews.Count,
                Brand = p.Brand ?? "",
                Status = p.Status ?? "Active",
                SubSubcategory = p.SubSubcategory,
                IsInStock = p.Stock > 0,
                Attributes = new Dictionary<string, string>()
            }).ToList();
        }

        #endregion
    }
} 