using Microsoft.AspNetCore.Mvc;
using CyberTech.Services;
using CyberTech.Models;

namespace CyberTech.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(IRecommendationService recommendationService, ILogger<RecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        /// <summary>
        /// Gợi ý sản phẩm dựa trên recently viewed từ localStorage
        /// </summary>
        /// <param name="request">Danh sách ID sản phẩm đã xem từ localStorage</param>
        /// <returns>Danh sách sản phẩm gợi ý</returns>
        [HttpPost("recently-viewed")]
        public async Task<ActionResult<RecommendationResponse>> GetRecommendationsFromRecentlyViewed([FromBody] RecommendationRequest request)
        {
            try
            {
                if (request?.ProductIds == null || !request.ProductIds.Any())
                {
                    return Ok(new RecommendationResponse
                    {
                        Success = true,
                        Products = new List<ProductViewModel>(),
                        Message = "No recently viewed products"
                    });
                }

                var recommendations = await _recommendationService.GetRecommendationsBasedOnRecentlyViewed(
                    request.ProductIds,
                    request.Limit ?? 8
                );

                return Ok(new RecommendationResponse
                {
                    Success = true,
                    Products = recommendations,
                    Message = $"Found {recommendations.Count} recommendations"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations from recently viewed");
                return Ok(new RecommendationResponse
                {
                    Success = false,
                    Products = new List<ProductViewModel>(),
                    Message = "Error loading recommendations"
                });
            }
        }

        /// <summary>
        /// Gợi ý sản phẩm cho abandoned cart
        /// </summary>
        /// <param name="request">Danh sách ID sản phẩm trong giỏ hàng</param>
        /// <returns>Danh sách sản phẩm gợi ý</returns>
        [HttpPost("abandoned-cart")]
        public async Task<ActionResult<RecommendationResponse>> GetAbandonedCartRecommendations([FromBody] RecommendationRequest request)
        {
            try
            {
                if (request?.ProductIds == null || !request.ProductIds.Any())
                {
                    return Ok(new RecommendationResponse
                    {
                        Success = true,
                        Products = new List<ProductViewModel>(),
                        Message = "No cart products"
                    });
                }

                var recommendations = await _recommendationService.GetRecommendationsForAbandonedCart(
                    request.ProductIds,
                    request.Limit ?? 8
                );

                return Ok(new RecommendationResponse
                {
                    Success = true,
                    Products = recommendations,
                    Message = $"Found {recommendations.Count} cart recommendations"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting abandoned cart recommendations");
                return Ok(new RecommendationResponse
                {
                    Success = false,
                    Products = new List<ProductViewModel>(),
                    Message = "Error loading cart recommendations"
                });
            }
        }

        /// <summary>
        /// Gợi ý cross-category products
        /// </summary>
        /// <param name="request">Danh sách ID sản phẩm gốc</param>
        /// <returns>Danh sách sản phẩm gợi ý cross-category</returns>
        [HttpPost("cross-category")]
        public async Task<ActionResult<RecommendationResponse>> GetCrossCategoryRecommendations([FromBody] RecommendationRequest request)
        {
            try
            {
                if (request?.ProductIds == null || !request.ProductIds.Any())
                {
                    return Ok(new RecommendationResponse
                    {
                        Success = true,
                        Products = new List<ProductViewModel>(),
                        Message = "No base products"
                    });
                }

                var recommendations = await _recommendationService.GetCrossCategoryRecommendations(
                    request.ProductIds,
                    request.Limit ?? 8
                );

                return Ok(new RecommendationResponse
                {
                    Success = true,
                    Products = recommendations,
                    Message = $"Found {recommendations.Count} cross-category recommendations"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cross-category recommendations");
                return Ok(new RecommendationResponse
                {
                    Success = false,
                    Products = new List<ProductViewModel>(),
                    Message = "Error loading cross-category recommendations"
                });
            }
        }

        /// <summary>
        /// Gợi ý sản phẩm tương tự
        /// </summary>
        /// <param name="request">Danh sách ID sản phẩm tham khảo</param>
        /// <returns>Danh sách sản phẩm tương tự</returns>
        [HttpPost("similar")]
        public async Task<ActionResult<RecommendationResponse>> GetSimilarProducts([FromBody] RecommendationRequest request)
        {
            try
            {
                if (request?.ProductIds == null || !request.ProductIds.Any())
                {
                    return Ok(new RecommendationResponse
                    {
                        Success = true,
                        Products = new List<ProductViewModel>(),
                        Message = "No reference products"
                    });
                }

                var recommendations = await _recommendationService.GetSimilarProducts(
                    request.ProductIds,
                    request.Limit ?? 8
                );

                return Ok(new RecommendationResponse
                {
                    Success = true,
                    Products = recommendations,
                    Message = $"Found {recommendations.Count} similar products"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar products");
                return Ok(new RecommendationResponse
                {
                    Success = false,
                    Products = new List<ProductViewModel>(),
                    Message = "Error loading similar products"
                });
            }
        }
    }

    // DTO classes
    public class RecommendationRequest
    {
        public List<int> ProductIds { get; set; } = new List<int>();
        public int? Limit { get; set; }
        public string? UserId { get; set; } // For future user-based recommendations
    }

    public class RecommendationResponse
    {
        public bool Success { get; set; }
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        public string Message { get; set; } = "";
        public int TotalCount => Products.Count;
    }
}