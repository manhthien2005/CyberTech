using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using CyberTech.Models;
using CyberTech.ViewModels;
using CyberTech.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text;

namespace CyberTech.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProductController> _logger;
        private readonly IDistributedCache _cache;
        private readonly HttpClient _httpClient;
        private const string CachePrefix = "ReviewSummary_";
        private const int CacheExpirationMinutes = 60;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;


        public ProductController(ApplicationDbContext context, IEmailService emailService, ILogger<ProductController> logger,
            IDistributedCache cache,
            HttpClient httpClient, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _cache = cache;
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // Hiển thị sản phẩm theo category
        public async Task<IActionResult> Category(string categoryId, ProductFilterModel filter)
        {
            var category = await _context.Categories
                .Include(c => c.Subcategories)
                    .ThenInclude(s => s.SubSubcategories)
                .FirstOrDefaultAsync(c => c.CategoryID.ToString() == categoryId);

            if (category == null)
                return NotFound();

            filter.CategoryId = categoryId;
            var viewModel = await BuildProductListViewModel(filter);
            viewModel.CategoryName = category.Name;
            viewModel.BreadcrumbPath = category.Name;

            return View("ProductList", viewModel);
        }

        // Hiển thị sản phẩm theo subcategory
        public async Task<IActionResult> Subcategory(string subcategoryId, ProductFilterModel filter)
        {
            var subcategory = await _context.Subcategories
                .Include(s => s.Category)
                .Include(s => s.SubSubcategories)
                .FirstOrDefaultAsync(s => s.SubcategoryID.ToString() == subcategoryId);

            if (subcategory == null)
                return NotFound();

            filter.SubcategoryId = subcategoryId;
            var viewModel = await BuildProductListViewModel(filter);
            viewModel.CategoryName = subcategory.Name;
            viewModel.BreadcrumbPath = $"{subcategory.Category.Name} / {subcategory.Name}";

            return View("ProductList", viewModel);
        }

        // Hiển thị sản phẩm theo subsubcategory
        public async Task<IActionResult> SubSubcategory(string subSubcategoryId, ProductFilterModel filter)
        {
            var subSubcategory = await _context.SubSubcategories
                .Include(ss => ss.Subcategory)
                    .ThenInclude(s => s.Category)
                .FirstOrDefaultAsync(ss => ss.SubSubcategoryID.ToString() == subSubcategoryId);

            if (subSubcategory == null)
                return NotFound();

            filter.SubSubcategoryId = subSubcategoryId;
            var viewModel = await BuildProductListViewModel(filter);
            viewModel.CategoryName = subSubcategory.Name;
            viewModel.BreadcrumbPath = $"{subSubcategory.Subcategory.Category.Name} / {subSubcategory.Subcategory.Name} / {subSubcategory.Name}";

            return View("ProductList", viewModel);
        }

        // Tìm kiếm sản phẩm
        public async Task<IActionResult> Search(ProductFilterModel filter)
        {
            if (string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                return RedirectToAction("Index", "Product");
            }

            var viewModel = await BuildSearchViewModel(filter);
            viewModel.CategoryName = $"Tìm kiếm: {filter.SearchQuery}";
            viewModel.BreadcrumbPath = "Kết quả tìm kiếm";

            return View("Search", viewModel);
        }

        private async Task<ProductListViewModel> BuildProductListViewModel(ProductFilterModel filter)
        {
            // Debug: Log filter attributes
            if (filter.Attributes != null && filter.Attributes.Any())
            {
                Console.WriteLine($"Filter Attributes Count: {filter.Attributes.Count}");
                foreach (var attr in filter.Attributes)
                {
                    Console.WriteLine($"Attribute: {attr.Key} = {attr.Value}");
                }
            }
            else
            {
                Console.WriteLine("No filter attributes found - checking query parameters directly");

                // Read attributes from query parameters directly
                var queryParams = Request.Query;
                var knownAttributeNames = new[] { "RAM", "CPU", "SSD", "Graphics Card", "Display", "OS", "LED RGB", "Kết nối" };

                foreach (var attrName in knownAttributeNames)
                {
                    if (queryParams.ContainsKey(attrName))
                    {
                        var attrValue = queryParams[attrName].ToString();
                        if (!string.IsNullOrEmpty(attrValue))
                        {
                            Console.WriteLine($"Found attribute in query: {attrName} = {attrValue}");
                            filter.Attributes[attrName] = attrValue;
                        }
                    }
                }

                if (filter.Attributes.Any())
                {
                    Console.WriteLine($"Added {filter.Attributes.Count} attributes from query parameters");
                }
            }

            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                .Include(p => p.Reviews)
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                .Where(p => p.Status == "Active");

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(filter.CategoryId))
            {
                query = query.Where(p => p.SubSubcategory.Subcategory.CategoryID.ToString() == filter.CategoryId);
            }

            if (!string.IsNullOrEmpty(filter.SubcategoryId))
            {
                query = query.Where(p => p.SubSubcategory.SubcategoryID.ToString() == filter.SubcategoryId);
            }

            if (!string.IsNullOrEmpty(filter.SubSubcategoryId))
            {
                query = query.Where(p => p.SubSubcategoryID.ToString() == filter.SubSubcategoryId);
            }

            // Lọc theo tìm kiếm
            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                query = query.Where(p => p.Name.Contains(filter.SearchQuery) ||
                                        p.Description.Contains(filter.SearchQuery) ||
                                        p.Brand.Contains(filter.SearchQuery));
            }

            // Lọc theo giá
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) <= filter.MaxPrice.Value);
            }

            // Lọc theo khuyến mãi
            if (filter.HasDiscount == true)
            {
                query = query.Where(p => p.SalePrice.HasValue && p.SalePrice < p.Price);
            }

            // Lọc theo thuộc tính
            foreach (var attr in filter.Attributes)
            {
                var attributeKey = attr.Key?.Trim();
                var attributeValue = attr.Value?.Trim();

                Console.WriteLine($"Filtering by attribute: '{attributeKey}' = '{attributeValue}'");

                var beforeCount = await query.CountAsync();
                Console.WriteLine($"Products before filtering by {attributeKey}: {beforeCount}");

                query = query.Where(p => p.ProductAttributeValues.Any(pav =>
                    pav.AttributeValue.ProductAttribute.AttributeName.Trim() == attributeKey &&
                    pav.AttributeValue.ValueName.Trim() == attributeValue));

                var afterCount = await query.CountAsync();
                Console.WriteLine($"Products after filtering by {attributeKey}: {afterCount}");

                // Debug: Check what values exist for this attribute
                var existingValues = await _context.ProductAttributeValues
                    .Include(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                    .Where(pav => pav.AttributeValue.ProductAttribute.AttributeName.Trim() == attributeKey)
                    .Select(pav => new
                    {
                        AttributeName = pav.AttributeValue.ProductAttribute.AttributeName,
                        ValueName = pav.AttributeValue.ValueName,
                        ProductId = pav.ProductID
                    })
                    .Distinct()
                    .ToListAsync();

                Console.WriteLine($"Existing values for '{attributeKey}':");
                foreach (var ev in existingValues.Take(10)) // Limit to first 10 for readability
                {
                    Console.WriteLine($"  - '{ev.ValueName}' (Product: {ev.ProductId})");
                }
            }

            // Sắp xếp
            query = filter.SortBy?.ToLower() switch
            {
                "price" => filter.SortOrder == "desc" ? query.OrderByDescending(p => p.SalePrice != null ? p.SalePrice.Value : p.Price) : query.OrderBy(p => p.SalePrice != null ? p.SalePrice.Value : p.Price),
                "name" => filter.SortOrder == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "rating" => query.OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0),
                "bestseller" => query.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity)),
                _ => query.OrderBy(p => p.Name)
            };

            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalProducts / filter.PageSize);

            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Lấy category ID hiện tại để load attributes
            int? currentCategoryId = null;
            int? currentSubcategoryId = null;
            int? currentSubSubcategoryId = null;
            if (!string.IsNullOrEmpty(filter.CategoryId))
            {
                currentCategoryId = int.Parse(filter.CategoryId);
            }
            else if (!string.IsNullOrEmpty(filter.SubcategoryId))
            {
                var subcategory = await _context.Subcategories.FindAsync(int.Parse(filter.SubcategoryId));
                currentCategoryId = subcategory?.CategoryID;
                currentSubcategoryId = int.Parse(filter.SubcategoryId);
            }
            else if (!string.IsNullOrEmpty(filter.SubSubcategoryId))
            {
                var subSubcategory = await _context.SubSubcategories
                    .Include(ss => ss.Subcategory)
                    .FirstOrDefaultAsync(ss => ss.SubSubcategoryID == int.Parse(filter.SubSubcategoryId));
                currentCategoryId = subSubcategory?.Subcategory.CategoryID;
                currentSubSubcategoryId = int.Parse(filter.SubSubcategoryId);
            }

            // Load CategoryAttributes cho category hiện tại
            var categoryAttributes = new List<CategoryAttributeFilter>();
            if (currentCategoryId.HasValue)
            {
                var categoryAttrs = await _context.CategoryAttributes
                    .Where(ca => ca.CategoryID == currentCategoryId.Value)
                    .ToListAsync();

                foreach (var categoryAttr in categoryAttrs)
                {
                    // Lấy values cho attribute này từ các sản phẩm 
                    // Ưu tiên từ phạm vi nhỏ nhất: SubSubcategory > Subcategory > Category
                    var attributeValues = await _context.ProductAttributeValues
                        .Include(pav => pav.AttributeValue)
                            .ThenInclude(av => av.ProductAttribute)
                        .Include(pav => pav.Product)
                            .ThenInclude(p => p.SubSubcategory)
                                .ThenInclude(ss => ss.Subcategory)
                        .Where(pav => pav.AttributeValue.ProductAttribute.AttributeName == categoryAttr.AttributeName &&
                                     pav.Product.Status == "Active" &&
                                     (currentSubSubcategoryId.HasValue ?
                                        pav.Product.SubSubcategoryID == currentSubSubcategoryId.Value :
                                        currentSubcategoryId.HasValue ?
                                        pav.Product.SubSubcategory.SubcategoryID == currentSubcategoryId.Value :
                                        pav.Product.SubSubcategory.Subcategory.CategoryID == currentCategoryId.Value))
                        .ToListAsync();

                    var groupedValues = attributeValues
                        .GroupBy(pav => pav.AttributeValue.ValueName)
                        .Select(g => new AttributeValueOption
                        {
                            Value = g.Key,
                            DisplayText = g.Key,
                            ProductCount = g.Select(pav => pav.Product.ProductID).Distinct().Count(), // Count unique products
                            IsSelected = filter.Attributes.ContainsKey(categoryAttr.AttributeName) &&
                                        filter.Attributes[categoryAttr.AttributeName] == g.Key
                        })
                        .Where(avo => avo.ProductCount > 0) // Only include values that have products
                        .OrderBy(avo => avo.DisplayText)
                        .ToList();

                    if (groupedValues.Any())
                    {
                        categoryAttributes.Add(new CategoryAttributeFilter
                        {
                            AttributeName = categoryAttr.AttributeName,
                            DisplayName = GetDisplayName(categoryAttr.AttributeName),
                            Values = groupedValues
                        });
                    }
                }
            }

            // Lấy giá min/max
            var allFilteredProducts = await _context.Products
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                .Where(p => p.Status == "Active")
                .Where(p => currentCategoryId == null || p.SubSubcategory.Subcategory.CategoryID == currentCategoryId)
                .ToListAsync();

            var priceRange = allFilteredProducts.Any() ?
                new { Min = allFilteredProducts.Min(p => p.SalePrice != null ? p.SalePrice.Value : p.Price), Max = allFilteredProducts.Max(p => p.SalePrice != null ? p.SalePrice.Value : p.Price) } :
                new { Min = 0m, Max = 0m };

            // Tạo ProductViewModel
            var productViewModels = products.Select(p => new CyberTech.Models.ProductViewModel
            {
                ProductID = p.ProductID,
                Name = p.Name ?? "",
                Price = p.Price,
                SalePrice = p.SalePrice.HasValue ? p.SalePrice.Value : (decimal?)null,
                SalePercentage = p.SalePercentage,
                DiscountedPrice = p.SalePrice,
                PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/default-product.jpg",
                PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/default-product.jpg",
                Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                Attributes = p.ProductAttributeValues.ToDictionary(
                    pav => pav.AttributeValue.ProductAttribute.AttributeName,
                    pav => pav.AttributeValue.ValueName
                ),
                AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews.Count(),
                Brand = p.Brand ?? "",
                Status = p.Status ?? "Active",
                SubSubcategory = p.SubSubcategory,
                IsInStock = p.Stock > 0
            }).ToList();

            return new ProductListViewModel
            {
                Products = productViewModels,
                Filter = filter,
                TotalProducts = totalProducts,
                TotalPages = totalPages,
                CurrentPage = filter.Page,
                CategoryAttributes = categoryAttributes,
                MinPrice = priceRange.Min,
                MaxPrice = priceRange.Max
            };
        }

        private decimal? GetDiscountedPrice(Product product)
        {
            var discountPriceAttr = product.ProductAttributeValues
                .FirstOrDefault(pav => pav.AttributeValue.ProductAttribute.AttributeName == "DiscountPrice");

            if (discountPriceAttr != null && decimal.TryParse(discountPriceAttr.AttributeValue.ValueName, out decimal discountPrice))
            {
                return discountPrice;
            }

            return null;
        }

        private string GetDisplayName(string attributeName)
        {
            return attributeName switch
            {
                "CPU" => "Bộ vi xử lý",
                "RAM" => "Bộ nhớ RAM",
                "SSD" => "Ổ cứng SSD",
                "Graphics Card" => "Card đồ họa",
                "Display" => "Màn hình",
                "OS" => "Hệ điều hành",
                "LED RGB" => "Đèn LED RGB",
                "Kết nối" => "Kết nối",
                _ => attributeName
            };
        }

        public async Task<IActionResult> ProductDetail(int id, int reviewPage = 1)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy sản phẩm liên quan với đầy đủ thông tin
            var relatedProducts = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                .Include(p => p.Reviews)
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                .Where(p => p.SubSubcategoryID == product.SubSubcategoryID && p.ProductID != id && p.Status == "Active")
                .Take(8)
                .ToListAsync();

            bool isInWishlist = false;
            bool isSubscribedToStock = false;
            bool canReview = false;
            bool hasUserReviewed = false;
            Review? userReview = null;

            if (User.Identity.IsAuthenticated)
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);
                if (emailClaim != null)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim.Value);
                    if (user != null)
                    {
                        isInWishlist = await _context.WishlistItems
                            .AnyAsync(wi => wi.UserID == user.UserID && wi.ProductID == id);

                        isSubscribedToStock = await _context.ProductStockNotifications
                            .AnyAsync(n => n.UserID == user.UserID && n.ProductID == id && n.IsActive);

                        // Kiểm tra user đã mua sản phẩm này chưa
                        var hasPurchased = await _context.OrderItems
                            .Include(oi => oi.Order)
                                .ThenInclude(o => o.Payments)
                            .AnyAsync(oi => oi.ProductID == id &&
                                           oi.Order.UserID == user.UserID &&
                                           oi.Order.Status == "Delivered" &&
                                           oi.Order.Payments.Any(p => p.PaymentStatus == "Completed"));

                        // Kiểm tra user đã đánh giá chưa
                        userReview = await _context.Reviews
                            .FirstOrDefaultAsync(r => r.UserID == user.UserID && r.ProductID == id);

                        canReview = hasPurchased && userReview == null;
                        hasUserReviewed = userReview != null;
                    }
                }
            }

            // Phân trang cho reviews
            const int reviewsPerPage = 5;
            var totalReviews = product.Reviews.Count();
            var totalPages = (int)Math.Ceiling(totalReviews / (double)reviewsPerPage);

            // Validate review page
            if (reviewPage < 1) reviewPage = 1;
            if (reviewPage > totalPages && totalPages > 0) reviewPage = totalPages;

            var paginatedReviews = product.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Skip((reviewPage - 1) * reviewsPerPage)
                .Take(reviewsPerPage)
                .ToList();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts,
                IsInWishlist = isInWishlist,
                IsSubscribedToStock = isSubscribedToStock,
                CanReview = canReview,
                HasUserReviewed = hasUserReviewed,
                UserReview = userReview,
                PaginatedReviews = paginatedReviews,
                ReviewsCurrentPage = reviewPage,
                ReviewsPerPage = reviewsPerPage,
                ReviewsTotalPages = totalPages,
                ReviewsTotalCount = totalReviews
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetReviewSummary(int productId)
        {
            try
            {
                string cacheKey = $"{CachePrefix}{productId}";
                string cachedSummary = await _cache.GetStringAsync(cacheKey);

                // Return cached summary if available
                if (!string.IsNullOrEmpty(cachedSummary))
                {
                    _logger.LogInformation("Retrieved review summary from cache for product ID: {ProductId}", productId);
                    return Json(new { success = true, summary = cachedSummary });
                }

                // Get product with reviews
                var product = await _context.Products
                    .Include(p => p.Reviews)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                string reviewSummary;
                if (!product.Reviews.Any())
                {
                    reviewSummary = "Chưa có đánh giá nào cho sản phẩm này.";
                }
                else
                {
                    // Calculate average rating correctly
                    var avgRating = product.Reviews.Average(r => r.Rating);

                    // Giới hạn số lượng review để tránh vượt giới hạn token của Gemini
                    var recentReviews = product.Reviews.OrderByDescending(r => r.CreatedAt).Take(50).ToList();
                    var reviewText = string.Join(" ", recentReviews.Select(r =>
                        $"Rating: {r.Rating}/5. Nội dung nhận xét: {(string.IsNullOrEmpty(r.Comment) ? "Không có nhận xét" : r.Comment)}"));
                    _logger.LogInformation("[LOG INFO] Reviews for product ID {ProductId}: {ReviewText}", productId, reviewText);

                    var systemPrompt = @"Bạn là một trợ lý AI chuyên nghiệp, có nhiệm vụ **tóm tắt đánh giá sản phẩm** từ người dùng một cách **ngắn gọn, dễ hiểu, chính xác và trung lập**. Mục tiêu là giúp khách hàng nhanh chóng nắm bắt điểm mạnh, điểm yếu của sản phẩm qua các phản hồi thực tế. Hãy:

- Tóm tắt thành 1 đoạn văn ngắn (60-100 từ).
- Nêu rõ các **điểm mạnh** (ưu điểm được nhiều người khen) và **điểm yếu** (nhược điểm được đề cập).
- Sử dụng ngôn ngữ tự nhiên, khách quan, không thêm thắt ý kiến cá nhân.
- Nếu có thông tin về đánh giá trung bình, hãy đề cập ngắn gọn.";

                    var fullPrompt = $@"{systemPrompt}

**DANH SÁCH REVIEW CỦA SẢN PHẨM {product.Name}**:
{reviewText}

**ĐÁNH GIÁ TRUNG BÌNH**: {avgRating:F1}/5 sao

**YÊU CẦU**:
Tóm tắt các review trên thành 1 đoạn văn ngắn, nêu rõ điểm mạnh và điểm yếu của sản phẩm. Đề cập đánh giá trung bình nếu có. Đảm bảo nội dung trung lập và súc tích.";

                    try
                    {
                        var httpClient = _httpClientFactory.CreateClient();
                        var apiKey = _configuration["GeminiSettings:ApiKey"];
                        var apiEndpoint = $"{_configuration["GeminiSettings:ApiEndpoint"]}?key={apiKey}";

                        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiEndpoint))
                        {
                            _logger.LogError("Gemini API configuration missing for product ID: {ProductId}", productId);
                            return Json(new { success = false, message = "Không thể tạo tóm tắt đánh giá do lỗi cấu hình." });
                        }

                        var requestBody = new
                        {
                            contents = new[]
                            {
                                new
                                {
                                    parts = new[]
                                    {
                                        new { text = fullPrompt }
                                    }
                                }
                            }
                        };

                        var requestContent = new StringContent(
                            JsonSerializer.Serialize(requestBody),
                            Encoding.UTF8,
                            "application/json");

                        var response = await httpClient.PostAsync(apiEndpoint, requestContent);

                        if (!response.IsSuccessStatusCode)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogError("Gemini API call failed for product ID: {ProductId}. Status: {StatusCode}, Error: {ErrorContent}",
                                productId, response.StatusCode, errorContent);
                            return Json(new { success = false, message = "Không thể tạo tóm tắt đánh giá do lỗi hệ thống." });
                        }

                        var responseBody = await response.Content.ReadFromJsonAsync<JsonElement>();
                        try
                        {
                            reviewSummary = responseBody
                                .GetProperty("candidates")[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text")
                                .GetString();

                            if (string.IsNullOrEmpty(reviewSummary))
                            {
                                _logger.LogWarning("Gemini API returned empty summary for product ID: {ProductId}", productId);
                                return Json(new { success = false, message = "Không thể tạo tóm tắt đánh giá do phản hồi rỗng." });
                            }

                            // Format the review summary to remove extra whitespace and ensure proper paragraphs
                            reviewSummary = reviewSummary.Trim()
                                .Replace("\r\n", "\n")
                                .Replace("\n\n\n", "\n\n")
                                .Replace("\n\n", "<br><br>")
                                .Replace("\n", "<br>");

                            // Cache the summary
                            var cacheOptions = new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
                            };
                            await _cache.SetStringAsync(cacheKey, reviewSummary, cacheOptions);
                            _logger.LogInformation("Stored review summary in cache for product ID: {ProductId}", productId);
                        }
                        catch (KeyNotFoundException ex)
                        {
                            _logger.LogError(ex, "Invalid Gemini API response structure for product ID: {ProductId}", productId);
                            return Json(new { success = false, message = "Không thể tạo tóm tắt đánh giá do lỗi định dạng dữ liệu." });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating review summary for product ID: {ProductId}", productId);
                        return Json(new { success = false, message = "Lỗi khi tạo tóm tắt đánh giá." });
                    }
                }

                return Json(new { success = true, summary = reviewSummary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReviewSummary for product ID: {ProductId}", productId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải tóm tắt đánh giá" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadReviews(int productId, int page = 1)
        {
            const int reviewsPerPage = 5;

            var product = await _context.Products
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            var totalReviews = product.Reviews.Count();
            var totalPages = (int)Math.Ceiling(totalReviews / (double)reviewsPerPage);

            // Validate page
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var paginatedReviews = product.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * reviewsPerPage)
                .Take(reviewsPerPage)
                .ToList();

            var reviewsHtml = await RenderPartialViewToStringAsync("_ReviewsList", paginatedReviews);
            var paginationHtml = await RenderPartialViewToStringAsync("_ReviewsPagination", new
            {
                CurrentPage = page,
                TotalPages = totalPages,
                ProductId = productId
            });

            return Json(new
            {
                success = true,
                reviewsHtml = reviewsHtml,
                paginationHtml = paginationHtml,
                currentPage = page,
                totalPages = totalPages,
                totalReviews = totalReviews
            });
        }

        private async Task<string> RenderPartialViewToStringAsync(string viewName, object model)
        {
            // Simple fallback - return JSON representation for AJAX
            if (viewName == "_ReviewsList")
            {
                var reviews = model as List<Review>;
                if (reviews == null) return "";

                var html = "";
                if (reviews.Any())
                {
                    foreach (var review in reviews)
                    {
                        var stars = "";
                        for (int i = 0; i < review.Rating; i++)
                            stars += "<i class=\"fas fa-star\"></i>";
                        for (int i = 0; i < 5 - review.Rating; i++)
                            stars += "<i class=\"far fa-star\"></i>";

                        html += $@"
                        <div class=""review-item"">
                            <div class=""reviewer-info"">
                                <div class=""reviewer-avatar"">
                                    <img src=""/placeholder.svg?height=50&width=50"" alt=""User Avatar"">
                                </div>
                                <div class=""reviewer-name-date"">
                                    <div class=""reviewer-name"">{review.User?.Name ?? "Người dùng"}</div>
                                    <div class=""review-date"">{review.CreatedAt:dd/MM/yyyy}</div>
                                </div>
                            </div>
                            <div class=""review-rating"">
                                {stars}
                            </div>
                            <div class=""review-content"">
                                <p>{(string.IsNullOrEmpty(review.Comment) ? "Người dùng không để lại nhận xét." : review.Comment)}</p>
                            </div>
                        </div>";
                    }
                }
                else
                {
                    html = @"<div class=""no-reviews""><p>Chưa có đánh giá nào cho sản phẩm này.</p></div>";
                }
                return html;
            }

            if (viewName == "_ReviewsPagination")
            {
                dynamic paginationModel = model;
                var currentPage = (int)paginationModel.CurrentPage;
                var totalPages = (int)paginationModel.TotalPages;
                var productId = (int)paginationModel.ProductId;

                if (totalPages <= 1) return "";

                var html = @"<div class=""reviews-pagination""><div class=""pagination-controls"">";

                // Previous button
                if (currentPage > 1)
                {
                    html += $@"<button class=""pagination-btn pagination-prev"" data-page=""{currentPage - 1}"" data-product-id=""{productId}"">
                        <i class=""fas fa-chevron-left""></i> Trước
                    </button>";
                }

                // Page numbers
                var startPage = Math.Max(1, currentPage - 2);
                var endPage = Math.Min(totalPages, currentPage + 2);

                if (startPage > 1)
                {
                    html += $@"<button class=""pagination-btn pagination-number"" data-page=""1"" data-product-id=""{productId}"">1</button>";
                    if (startPage > 2)
                        html += @"<span class=""pagination-dots"">...</span>";
                }

                for (int i = startPage; i <= endPage; i++)
                {
                    var activeClass = i == currentPage ? " active" : "";
                    html += $@"<button class=""pagination-btn pagination-number{activeClass}"" data-page=""{i}"" data-product-id=""{productId}"">{i}</button>";
                }

                if (endPage < totalPages)
                {
                    if (endPage < totalPages - 1)
                        html += @"<span class=""pagination-dots"">...</span>";
                    html += $@"<button class=""pagination-btn pagination-number"" data-page=""{totalPages}"" data-product-id=""{productId}"">{totalPages}</button>";
                }

                // Next button
                if (currentPage < totalPages)
                {
                    html += $@"<button class=""pagination-btn pagination-next"" data-page=""{currentPage + 1}"" data-product-id=""{productId}"">
                        Sau <i class=""fas fa-chevron-right""></i>
                    </button>";
                }

                html += $@"</div><div class=""pagination-info"">Trang {currentPage} / {totalPages}</div></div>";

                return html;
            }

            return "";
        }

        public async Task<IActionResult> DebugBinding(ProductFilterModel filter)
        {
            var debugInfo = new
            {
                CategoryId = filter.CategoryId,
                SubcategoryId = filter.SubcategoryId,
                SubSubcategoryId = filter.SubSubcategoryId,
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,
                HasDiscount = filter.HasDiscount,
                SearchQuery = filter.SearchQuery,
                AttributesCount = filter.Attributes?.Count ?? 0,
                Attributes = filter.Attributes?.ToDictionary(a => a.Key, a => a.Value) ?? new Dictionary<string, string>(),
                RawQueryString = Request.QueryString.Value
            };

            return Json(debugInfo);
        }

        public async Task<IActionResult> DebugAttributes(int categoryId = 1)
        {
            var categoryAttributes = await _context.CategoryAttributes
                .Where(ca => ca.CategoryID == categoryId)
                .ToListAsync();

            var result = new List<object>();

            foreach (var categoryAttr in categoryAttributes)
            {
                var attributeValues = await _context.ProductAttributeValues
                    .Include(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                    .Include(pav => pav.Product)
                        .ThenInclude(p => p.SubSubcategory)
                            .ThenInclude(ss => ss.Subcategory)
                    .Where(pav => pav.AttributeValue.ProductAttribute.AttributeName == categoryAttr.AttributeName &&
                                 pav.Product.SubSubcategory.Subcategory.CategoryID == categoryId &&
                                 pav.Product.Status == "Active")
                    .Select(pav => new
                    {
                        AttributeName = pav.AttributeValue.ProductAttribute.AttributeName,
                        ValueName = pav.AttributeValue.ValueName,
                        ProductId = pav.Product.ProductID,
                        ProductName = pav.Product.Name
                    })
                    .ToListAsync();

                result.Add(new
                {
                    AttributeName = categoryAttr.AttributeName,
                    Values = attributeValues.GroupBy(av => av.ValueName)
                        .Select(g => new
                        {
                            Value = g.Key,
                            Count = g.Count(),
                            Products = g.Select(x => new { x.ProductId, x.ProductName }).ToList()
                        }).ToList()
                });
            }

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string query, int limit = 15)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Json(new { success = false, message = "Query too short" });
                }

                var products = await _context.Products
                    .Where(p => p.Status == "Active" &&
                               p.Stock > 0 &&
                               p.Name.Contains(query))
                    .OrderByDescending(p => p.Name.StartsWith(query) ? 1 : 0)
                    .ThenBy(p => p.Name)
                    .Take(limit)
                    .ToListAsync();

                var suggestions = new List<object>();

                foreach (var product in products)
                {
                    var primaryImage = await _context.ProductImages
                        .Where(pi => pi.ProductID == product.ProductID && pi.IsPrimary)
                        .FirstOrDefaultAsync();

                    var imageUrl = primaryImage?.ImageURL ?? "/images/no-image.png";

                    // Create simple object with explicit properties
                    var suggestion = new
                    {
                        id = product.ProductID,
                        name = product.Name,
                        price = product.Price,
                        salePrice = product.SalePrice,
                        salePercentage = product.SalePercentage,
                        image = imageUrl,
                        // Computed properties từ logic mới với Round Down
                        currentPrice = product.SalePrice.HasValue && product.SalePrice > 0
                                      ? product.SalePrice.Value
                                      : (product.SalePercentage.HasValue && product.SalePercentage > 0
                                         ? Math.Floor((product.Price * (1 - product.SalePercentage.Value / 100)) / 1000) * 1000
                                         : product.Price),
                        hasSale = (product.SalePrice.HasValue && product.SalePrice > 0 && product.SalePrice < product.Price) ||
                                 (product.SalePercentage.HasValue && product.SalePercentage > 0)
                    };

                    suggestions.Add(suggestion);
                }

                return Json(new { success = true, data = suggestions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<ProductListViewModel> BuildSearchViewModel(ProductFilterModel filter)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                .Include(p => p.Reviews)
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                .Where(p => p.Status == "Active");

            // Search by main query
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var searchTerms = filter.SearchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    query = query.Where(p => p.Name.Contains(term) ||
                                           p.Description.Contains(term) ||
                                           p.Brand.Contains(term));
                }
            }

            // Refine search
            if (!string.IsNullOrWhiteSpace(filter.RefineQuery))
            {
                query = query.Where(p => p.Name.Contains(filter.RefineQuery) ||
                                       p.Description.Contains(filter.RefineQuery));
            }

            // Filter by categories
            if (filter.Categories != null && filter.Categories.Any())
            {
                var categoryIds = filter.Categories.Select(int.Parse).ToList();
                query = query.Where(p => categoryIds.Contains(p.SubSubcategory.Subcategory.CategoryID));
            }

            // Filter by brands
            if (filter.Brands != null && filter.Brands.Any())
            {
                query = query.Where(p => filter.Brands.Contains(p.Brand));
            }

            // Price filter
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => (p.SalePrice != null ? p.SalePrice.Value : p.Price) <= filter.MaxPrice.Value);
            }

            // Discount filter
            if (filter.HasDiscount == true)
            {
                query = query.Where(p => p.SalePrice.HasValue && p.SalePrice < p.Price);
            }

            // Stock filter
            if (filter.InStock == true)
            {
                query = query.Where(p => p.Stock > 0);
            }

            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "relevance" => query.OrderByDescending(p => p.Name.StartsWith(filter.SearchQuery != null ? filter.SearchQuery : "") ? 2 :
                                                           p.Name.Contains(filter.SearchQuery != null ? filter.SearchQuery : "") ? 1 : 0)
                                   .ThenBy(p => p.Name),
                "price" when filter.SortOrder == "desc" => query.OrderByDescending(p => p.SalePrice != null ? p.SalePrice.Value : p.Price),
                "price" => query.OrderBy(p => p.SalePrice != null ? p.SalePrice.Value : p.Price),
                "name" when filter.SortOrder == "desc" => query.OrderByDescending(p => p.Name),
                "name" => query.OrderBy(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "rating" => query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
                "bestseller" => query.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity)),
                _ => query.OrderByDescending(p => p.Name.StartsWith(filter.SearchQuery != null ? filter.SearchQuery : "") ? 2 :
                                           p.Name.Contains(filter.SearchQuery != null ? filter.SearchQuery : "") ? 1 : 0)
                           .ThenBy(p => p.Name)
            };

            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)filter.PageSize);

            // Get price range for filters
            var priceRange = await query
                .Select(p => p.SalePrice != null ? p.SalePrice.Value : p.Price)
                .DefaultIfEmpty()
                .GroupBy(x => 1)
                .Select(g => new { Min = g.Min(), Max = g.Max() })
                .FirstOrDefaultAsync() ?? new { Min = 0m, Max = 0m };

            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Build category attributes for filters (from search results)
            var categoryAttributes = await BuildSearchCategoryAttributes(query, filter);

            // Create ProductViewModels
            var productViewModels = products.Select(p => new CyberTech.Models.ProductViewModel
            {
                ProductID = p.ProductID,
                Name = p.Name ?? "",
                Price = p.Price,
                SalePrice = p.SalePrice.HasValue ? p.SalePrice.Value : (decimal?)null,
                SalePercentage = p.SalePercentage,
                DiscountedPrice = p.SalePrice,
                PrimaryImageUrl = p.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL ??
                                p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/no-image.png",
                PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL ??
                                     p.ProductImages.FirstOrDefault()?.ImageURL ?? "/images/no-image.png",
                Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                Attributes = p.ProductAttributeValues.ToDictionary(
                    pav => pav.AttributeValue.ProductAttribute.AttributeName,
                    pav => pav.AttributeValue.ValueName
                ),
                AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews.Count(),
                Brand = p.Brand ?? "",
                Status = p.Status ?? "Active",
                SubSubcategory = p.SubSubcategory,
                IsInStock = p.Stock > 0
            }).ToList();

            return new ProductListViewModel
            {
                Products = productViewModels,
                Filter = filter,
                TotalProducts = totalProducts,
                TotalPages = totalPages,
                CurrentPage = filter.Page,
                CategoryAttributes = categoryAttributes,
                MinPrice = priceRange.Min,
                MaxPrice = priceRange.Max
            };
        }

        private async Task<List<CategoryAttributeFilter>> BuildSearchCategoryAttributes(IQueryable<Product> searchQuery, ProductFilterModel filter)
        {
            var categoryAttributes = new List<CategoryAttributeFilter>();

            // Get categories from search results
            var categories = await searchQuery
                .Select(p => new
                {
                    p.SubSubcategory.Subcategory.Category.CategoryID,
                    p.SubSubcategory.Subcategory.Category.Name
                })
                .Distinct()
                .GroupBy(c => new { c.CategoryID, c.Name })
                .Select(g => new AttributeValueOption
                {
                    Value = g.Key.CategoryID.ToString(),
                    DisplayText = g.Key.Name,
                    ProductCount = g.Count(),
                    IsSelected = filter.Categories != null && filter.Categories.Contains(g.Key.CategoryID.ToString())
                })
                .ToListAsync();

            if (categories.Any())
            {
                categoryAttributes.Add(new CategoryAttributeFilter
                {
                    AttributeName = "Category",
                    DisplayName = "Danh mục",
                    Values = categories.OrderBy(c => c.DisplayText)
                });
            }

            // Get brands from search results
            var brands = await searchQuery
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .GroupBy(p => p.Brand)
                .Select(g => new AttributeValueOption
                {
                    Value = g.Key,
                    DisplayText = g.Key,
                    ProductCount = g.Count(),
                    IsSelected = filter.Brands != null && filter.Brands.Contains(g.Key)
                })
                .OrderBy(b => b.DisplayText)
                .ToListAsync();

            if (brands.Any())
            {
                categoryAttributes.Add(new CategoryAttributeFilter
                {
                    AttributeName = "Brand",
                    DisplayName = "Thương hiệu",
                    Values = brands
                });
            }

            return categoryAttributes;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStockNotification(int productId)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                if (product.Stock > 0)
                {
                    return Json(new { success = false, message = "Sản phẩm hiện đang có hàng" });
                }

                var existingNotification = await _context.ProductStockNotifications
                    .FirstOrDefaultAsync(n => n.ProductID == productId && n.UserID == user.UserID);

                if (existingNotification != null)
                {
                    _context.ProductStockNotifications.Remove(existingNotification);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Đã hủy đăng ký nhận thông báo" });
                }

                var notification = new ProductStockNotification
                {
                    ProductID = productId,
                    UserID = user.UserID,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.ProductStockNotifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã đăng ký nhận thông báo khi có hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý yêu cầu" });
            }
        }

        public async Task<ProductDetailViewModel> BuildProductDetailViewModel(Product product)
        {
            var isInWishlist = false;
            var isSubscribedToStock = false;

            if (User.Identity.IsAuthenticated)
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim != null)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim.Value);
                    if (user != null)
                    {
                        isInWishlist = await _context.WishlistItems
                            .AnyAsync(wi => wi.UserID == user.UserID && wi.ProductID == product.ProductID);

                        isSubscribedToStock = await _context.ProductStockNotifications
                            .AnyAsync(n => n.UserID == user.UserID && n.ProductID == product.ProductID && n.IsActive);
                    }
                }
            }

            var relatedProducts = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                .Include(p => p.Reviews)
                .Include(p => p.SubSubcategory)
                    .ThenInclude(ss => ss.Subcategory)
                        .ThenInclude(s => s.Category)
                .Where(p => p.SubSubcategoryID == product.SubSubcategoryID && p.ProductID != product.ProductID && p.Status == "Active")
                .Take(8)
                .ToListAsync();

            return new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts,
                IsInWishlist = isInWishlist,
                IsSubscribedToStock = isSubscribedToStock
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá" });
                }

                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    return Json(new { success = false, message = "Đánh giá phải từ 1 đến 5 sao" });
                }

                // Get current user
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không thể xác thực người dùng" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                // Kiểm tra user đã mua sản phẩm này chưa
                var hasPurchased = await _context.OrderItems
                    .Include(oi => oi.Order)
                        .ThenInclude(o => o.Payments)
                    .AnyAsync(oi => oi.ProductID == productId &&
                                   oi.Order.UserID == user.UserID &&
                                   oi.Order.Status == "Delivered" &&
                                   oi.Order.Payments.Any(p => p.PaymentStatus == "Completed"));

                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "Bạn chỉ có thể đánh giá sản phẩm đã mua" });
                }

                // Kiểm tra user đã đánh giá chưa
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.UserID == user.UserID && r.ProductID == productId);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "Bạn đã đánh giá sản phẩm này rồi" });
                }

                // Tạo đánh giá mới
                var review = new Review
                {
                    UserID = user.UserID,
                    ProductID = productId,
                    Rating = rating,
                    Comment = comment?.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đánh giá của bạn đã được gửi thành công!",
                    review = new
                    {
                        userName = user.Name,
                        rating = review.Rating,
                        comment = review.Comment,
                        createdAt = review.CreatedAt.ToString("dd/MM/yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(int productId, int rating, string comment)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để sửa đánh giá" });
                }

                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    return Json(new { success = false, message = "Đánh giá phải từ 1 đến 5 sao" });
                }

                // Get current user
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không thể xác thực người dùng" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                // Kiểm tra user đã mua sản phẩm này chưa
                var hasPurchased = await _context.OrderItems
                    .Include(oi => oi.Order)
                        .ThenInclude(o => o.Payments)
                    .AnyAsync(oi => oi.ProductID == productId &&
                                   oi.Order.UserID == user.UserID &&
                                   oi.Order.Status == "Delivered" &&
                                   oi.Order.Payments.Any(p => p.PaymentStatus == "Completed"));

                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "Bạn chỉ có thể sửa đánh giá sản phẩm đã mua" });
                }

                // Tìm đánh giá hiện tại
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.UserID == user.UserID && r.ProductID == productId);

                if (existingReview == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá để sửa" });
                }

                // Cập nhật đánh giá (tính như đánh giá mới)
                existingReview.Rating = rating;
                existingReview.Comment = comment?.Trim();
                existingReview.CreatedAt = DateTime.Now; // Cập nhật thời gian mới

                _context.Reviews.Update(existingReview);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đánh giá của bạn đã được cập nhật thành công!",
                    review = new
                    {
                        userName = user.Name,
                        rating = existingReview.Rating,
                        comment = existingReview.Comment,
                        createdAt = existingReview.CreatedAt.ToString("dd/MM/yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}