using CyberTech.Models;

namespace CyberTech.Services
{
    public interface IRecommendationService
    {
        /// <summary>
        /// Gợi ý sản phẩm dựa trên recently viewed products từ localStorage
        /// </summary>
        /// <param name="recentlyViewedIds">Danh sách ID sản phẩm đã xem</param>
        /// <param name="limit">Số lượng sản phẩm gợi ý</param>
        /// <returns>Danh sách sản phẩm gợi ý</returns>
        Task<List<ProductViewModel>> GetRecommendationsBasedOnRecentlyViewed(List<int> recentlyViewedIds, int limit = 8);

        /// <summary>
        /// Gợi ý sản phẩm cho abandoned cart items
        /// </summary>
        /// <param name="cartProductIds">Danh sách ID sản phẩm trong giỏ hàng</param>
        /// <param name="limit">Số lượng sản phẩm gợi ý</param>
        /// <returns>Danh sách sản phẩm gợi ý</returns>
        Task<List<ProductViewModel>> GetRecommendationsForAbandonedCart(List<int> cartProductIds, int limit = 8);

        /// <summary>
        /// Gợi ý sản phẩm Cross-Category (sản phẩm đi kèm)
        /// </summary>
        /// <param name="baseProductIds">Danh sách ID sản phẩm gốc</param>
        /// <param name="limit">Số lượng sản phẩm gợi ý</param>
        /// <returns>Danh sách sản phẩm gợi ý</returns>
        Task<List<ProductViewModel>> GetCrossCategoryRecommendations(List<int> baseProductIds, int limit = 8);

        /// <summary>
        /// Gợi ý sản phẩm tương tự dựa trên content (cùng category, brand, price range)
        /// </summary>
        /// <param name="productIds">Danh sách ID sản phẩm tham khảo</param>
        /// <param name="limit">Số lượng sản phẩm gợi ý</param>
        /// <returns>Danh sách sản phẩm gợi ý</returns>
        Task<List<ProductViewModel>> GetSimilarProducts(List<int> productIds, int limit = 8);
    }
} 