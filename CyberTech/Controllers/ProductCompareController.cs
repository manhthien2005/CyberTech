using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Models.DTOs;
using System.Text.Json;

namespace CyberTech.Controllers
{
    public class ProductCompareController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string COMPARE_SESSION_KEY = "CompareProducts";
        private const int MAX_COMPARE_PRODUCTS = 4;

        public ProductCompareController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var compareProductIds = GetCompareProductIds();
            var viewModel = new ProductCompareViewModel();

            if (compareProductIds.Any())
            {
                // Load products with all necessary data
                var products = await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductAttributeValues)
                        .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                    .Include(p => p.SubSubcategory)
                        .ThenInclude(ssc => ssc.Subcategory)
                        .ThenInclude(sc => sc.Category)
                    .Include(p => p.Reviews)
                    .Where(p => compareProductIds.Contains(p.ProductID))
                    .ToListAsync();

                if (products.Any())
                {
                    viewModel.Products = products;
                    viewModel.ComparisonType = GetComparisonType(products.First());
                    viewModel.CriteriaByCategory = GetComparisonCriteria(viewModel.ComparisonType);
                    viewModel.Analysis = await GenerateProductAnalysis(products, viewModel.ComparisonType);
                    
                    // Load suggested products of the same category
                    viewModel.SuggestedProducts = await GetSuggestedProducts(products.First().SubSubcategoryID, compareProductIds);
                    
                    // Generate technical specs for comparison
                    viewModel.TechnicalSpecs = GetTechnicalSpecsForComparison(products, viewModel.ComparisonType);
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCompare(int productId)
        {
            try
            {
                var compareProductIds = GetCompareProductIds();

                // Check if product already in compare list
                if (compareProductIds.Contains(productId))
                {
                    return Json(new { success = false, message = "Sản phẩm đã có trong danh sách so sánh" });
                }

                // Check maximum products limit
                if (compareProductIds.Count >= MAX_COMPARE_PRODUCTS)
                {
                    return Json(new { success = false, message = $"Chỉ có thể so sánh tối đa {MAX_COMPARE_PRODUCTS} sản phẩm" });
                }

                var product = await _context.Products
                    .Include(p => p.SubSubcategory)
                        .ThenInclude(ssc => ssc.Subcategory)
                        .ThenInclude(sc => sc.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                // Check product type compatibility if there are existing products
                if (compareProductIds.Any())
                {
                    var existingProduct = await _context.Products
                        .Include(p => p.SubSubcategory)
                            .ThenInclude(ssc => ssc.Subcategory)
                            .ThenInclude(sc => sc.Category)
                        .FirstOrDefaultAsync(p => p.ProductID == compareProductIds.First());

                    if (existingProduct != null && !AreProductsCompatible(existingProduct, product))
                    {
                        var existingType = GetComparisonType(existingProduct);
                        var newType = GetComparisonType(product);
                        return Json(new { 
                            success = false, 
                            message = $"Chỉ có thể so sánh các sản phẩm cùng loại. Hiện tại đang so sánh {existingType}, không thể thêm {newType}" 
                        });
                    }
                }

                // Add to compare list
                compareProductIds.Add(productId);
                SetCompareProductIds(compareProductIds);

                return Json(new { 
                    success = true, 
                    message = "Đã thêm vào danh sách so sánh",
                    compareCount = compareProductIds.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCompare(int productId)
        {
            try
            {
                var compareProductIds = GetCompareProductIds();
                compareProductIds.Remove(productId);
                SetCompareProductIds(compareProductIds);

                return Json(new { 
                    success = true, 
                    message = "Đã xóa khỏi danh sách so sánh",
                    compareCount = compareProductIds.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCompare()
        {
            HttpContext.Session.Remove(COMPARE_SESSION_KEY);
            return Json(new { success = true, message = "Đã xóa tất cả sản phẩm khỏi danh sách so sánh" });
        }

        [HttpGet]
        public async Task<IActionResult> GetCompareProducts()
        {
            var compareProductIds = GetCompareProductIds();
            var products = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => compareProductIds.Contains(p.ProductID))
                .Select(p => new {
                    productId = p.ProductID,
                    name = p.Name,
                    image = p.ProductImages.FirstOrDefault(pi => pi.IsPrimary).ImageURL,
                    price = p.GetEffectivePrice()
                })
                .ToListAsync();

            return Json(new { products = products, count = products.Count });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessAIChat([FromBody] CompareAIChatRequest request)
        {
            try
            {
                var productIds = GetCompareProductIds();
                var products = await _context.Products
                    .Include(p => p.Reviews)
                    .Include(p => p.ProductAttributeValues)
                        .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                    .Where(p => productIds.Contains(p.ProductID))
                    .ToListAsync();

                var response = ProcessUserQuery(request.Query, products);
                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new { 
                    type = "error",
                    message = "Có lỗi xảy ra khi xử lý yêu cầu. Vui lòng thử lại.",
                    recommendation = (object)null
                });
            }
        }

        private object ProcessUserQuery(string query, List<Product> products)
        {
            var queryLower = query.ToLower().Trim();
            
            // Input validation
            if (string.IsNullOrWhiteSpace(query))
            {
                return new { 
                    type = "warning",
                    message = "⚠️ Vui lòng nhập câu hỏi hoặc yêu cầu tư vấn của bạn.",
                    recommendation = (object)null
                };
            }

            // Check if we have products to compare
            if (!products.Any())
            {
                return new { 
                    type = "warning",
                    message = "⚠️ Bạn chưa thêm sản phẩm nào vào danh sách so sánh. Hãy thêm ít nhất 2 sản phẩm để AI có thể tư vấn.",
                    recommendation = (object)null
                };
            }

            if (products.Count == 1)
            {
                var singleProduct = products[0];
                return new { 
                    type = "info",
                    message = $"ℹ️ Bạn chỉ có 1 sản phẩm ({singleProduct.Name}) trong danh sách. Hãy thêm thêm sản phẩm để so sánh và AI tư vấn tốt hơn. Dưới đây là thông tin sản phẩm hiện tại:",
                    recommendation = new {
                        ProductID = singleProduct.ProductID,
                        Name = singleProduct.Name,
                        Price = singleProduct.GetEffectivePrice(),
                        Brand = singleProduct.Brand,
                        Score = 8m,
                        Reasons = new[] { "Sản phẩm duy nhất trong danh sách so sánh", $"Giá: {singleProduct.GetEffectivePrice() / 1000000:F1} triệu VND", $"Thương hiệu: {singleProduct.Brand}" },
                        ContextAnalysis = "Cần thêm sản phẩm khác để so sánh chi tiết"
                    }
                };
            }
            
            try
            {
                // Advanced product analysis based on user intent
                var analysis = AnalyzeProductsForQuery(products, query);
                
                if (analysis.BestMatch == null && analysis.AlternativeRecommendation == null)
                {
                    return new { 
                        type = "error",
                        message = "❌ Có lỗi xảy ra trong quá trình phân tích. Vui lòng thử lại.",
                        recommendation = (object)null
                    };
                }

                var recommendation = analysis.BestMatch ?? analysis.AlternativeRecommendation;
                
                return new { 
                    type = analysis.BestMatch != null ? "success" : "info",
                    message = analysis.Message,
                    recommendation = recommendation
                };
            }
            catch (Exception ex)
            {
                // Log error in production
                return new { 
                    type = "error",
                    message = "❌ Đã xảy ra lỗi trong quá trình phân tích. Vui lòng thử lại sau.",
                    recommendation = (object)null
                };
            }
        }

        private (dynamic BestMatch, dynamic AlternativeRecommendation, string Message) AnalyzeProductsForQuery(List<Product> products, string query)
        {
            var queryLower = query.ToLower();
            
            // Extract user requirements
            var requirements = ExtractUserRequirements(queryLower);
            
            // Score each product against requirements
            var scoredProducts = products.Select(product => {
                var analysis = AnalyzeProductForRequirements(product, requirements, products);
                return new {
                    ProductID = product.ProductID,
                    Name = product.Name,
                    Price = product.GetEffectivePrice(),
                    Brand = product.Brand,
                    Score = analysis.Score,
                    Reasons = analysis.Reasons,
                    ContextAnalysis = analysis.ContextAnalysis
                };
            }).OrderByDescending(x => x.Score).ToList();
            
                         var bestProduct = scoredProducts.First();
             var message = GenerateComparisonMessage(bestProduct, scoredProducts.Cast<object>().ToList(), requirements);
            
            // Check if best product is good enough
            if (bestProduct.Score >= 7)
            {
                return (bestProduct, null, message);
            }
            else
            {
                return (null, bestProduct, message);
            }
        }

        private Dictionary<string, object> ExtractUserRequirements(string query)
        {
            var requirements = new Dictionary<string, object>();
            var queryLower = query.ToLower();
            
            // Advanced budget extraction with multiple patterns
            var budgetPatterns = new[]
            {
                @"(dưới|dưới|dự án|ngân sách|tối đa)\s*(\d+)\s*(triệu|tr|k)",
                @"(trên|từ|ít nhất)\s*(\d+)\s*(triệu|tr|k)",
                @"(khoảng|trong khoảng|quanh)\s*(\d+)\s*(triệu|tr|k)",
                @"(\d+)\s*(triệu|tr|k)\s*(trở xuống|trở lên|đến\s*\d+)",
                @"(\d+)\s*-\s*(\d+)\s*(triệu|tr|k)",
                @"(\d+)\s*(triệu|tr|k)"
            };

            foreach (var pattern in budgetPatterns)
            {
                var budgetMatch = System.Text.RegularExpressions.Regex.Match(queryLower, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (budgetMatch.Success)
                {
                    var amountStr = budgetMatch.Groups[2].Success ? budgetMatch.Groups[2].Value : budgetMatch.Groups[1].Value;
                    if (decimal.TryParse(amountStr, out var amount))
                    {
                        var multiplier = budgetMatch.Groups[3].Value.ToLower() == "k" ? 1000 : 1000000;
                        var totalAmount = amount * multiplier;
                        
                        var operator_ = budgetMatch.Groups[1].Value.ToLower();
                        if (operator_.Contains("dưới") || operator_.Contains("tối đa") || operator_.Contains("dự án"))
                            operator_ = "dưới";
                        else if (operator_.Contains("trên") || operator_.Contains("từ") || operator_.Contains("ít nhất"))
                            operator_ = "trên";
                        else if (operator_.Contains("khoảng") || operator_.Contains("quanh"))
                            operator_ = "khoảng";
                        else
                            operator_ = "khoảng";

                        requirements["budget"] = new { amount = totalAmount, operator_ };
                        break;
                    }
                }
            }
            
            // Enhanced use case detection with more keywords
            var useCases = new Dictionary<string, string[]>
            {
                ["gaming"] = new[] { "chơi game", "gaming", "game", "geforce", "rtx", "fps", "stream", "esports" },
                ["study"] = new[] { "học", "học tập", "sinh viên", "đại học", "học online", "zoom", "teams" },
                ["office"] = new[] { "văn phòng", "làm việc", "công sở", "excel", "word", "powerpoint", "meeting" },
                ["design"] = new[] { "thiết kế", "đồ họa", "photoshop", "illustrator", "render", "3d", "video edit", "creative" },
                ["programming"] = new[] { "lập trình", "code", "developer", "coding", "ide", "visual studio" }
            };

            foreach (var useCase in useCases)
            {
                if (useCase.Value.Any(keyword => queryLower.Contains(keyword)))
                {
                    requirements["useCase"] = useCase.Key;
                    break;
                }
            }
            
            // Enhanced physical and performance requirements
            var physicalRequirements = new Dictionary<string, string[]>
            {
                ["portable"] = new[] { "nhẹ", "mang đi", "di động", "compact", "slim", "mỏng", "travel" },
                ["longBattery"] = new[] { "pin lâu", "pin tốt", "battery", "sạc lâu", "tiết kiệm pin" },
                ["bigScreen"] = new[] { "màn hình lớn", "screen lớn", "17 inch", "15.6", "wide screen" },
                ["performance"] = new[] { "mạnh", "nhanh", "hiệu năng cao", "performance", "tốc độ" },
                ["quiet"] = new[] { "yên tĩnh", "không ồn", "silent", "quiet", "fan nhỏ" },
                ["durable"] = new[] { "bền", "chắc chắn", "military grade", "thép", "nhôm" }
            };

            foreach (var req in physicalRequirements)
            {
                if (req.Value.Any(keyword => queryLower.Contains(keyword)))
                {
                    requirements[req.Key] = true;
                }
            }
            
            // Enhanced brand preference with Vietnamese and English names
            var brands = new Dictionary<string, string[]>
            {
                ["Apple"] = new[] { "apple", "macbook", "iphone", "mac" },
                ["Dell"] = new[] { "dell", "inspiron", "xps", "alienware" },
                ["HP"] = new[] { "hp", "pavilion", "envy", "omen", "elitebook" },
                ["Lenovo"] = new[] { "lenovo", "thinkpad", "legion", "ideapad" },
                ["Asus"] = new[] { "asus", "rog", "zenbook", "vivobook", "tuf" },
                ["MSI"] = new[] { "msi", "gaming", "stealth", "prestige" },
                ["Acer"] = new[] { "acer", "aspire", "predator", "swift" },
                ["Razer"] = new[] { "razer", "blade", "deathadder", "viper" },
                ["Logitech"] = new[] { "logitech", "logi", "mx", "g pro" },
                ["Corsair"] = new[] { "corsair", "k70", "dark core", "void" }
            };

            foreach (var brand in brands)
            {
                if (brand.Value.Any(keyword => queryLower.Contains(keyword)))
                {
                    requirements["preferredBrand"] = brand.Key.ToLower();
                    break;
                }
            }

            // Priority detection
            var priorities = new Dictionary<string, string[]>
            {
                ["price"] = new[] { "giá rẻ", "tiết kiệm", "budget", "cheap", "rẻ nhất" },
                ["performance"] = new[] { "hiệu năng", "mạnh nhất", "nhanh nhất", "performance", "powerful" },
                ["brand"] = new[] { "thương hiệu", "uy tín", "brand", "nổi tiếng" },
                ["design"] = new[] { "đẹp", "design", "thiết kế đẹp", "ngoại hình" }
            };

            foreach (var priority in priorities)
            {
                if (priority.Value.Any(keyword => queryLower.Contains(keyword)))
                {
                    requirements["priority"] = priority.Key;
                    break;
                }
            }
            
            return requirements;
        }

        private (decimal Score, List<string> Reasons, string ContextAnalysis) AnalyzeProductForRequirements(
            Product product, Dictionary<string, object> requirements, List<Product> allProducts)
        {
            decimal score = 6; // Base score
            var reasons = new List<string>();
            var contextAnalysis = "";
            
            // Budget analysis
            if (requirements.ContainsKey("budget"))
            {
                var budget = (dynamic)requirements["budget"];
                var price = product.GetEffectivePrice();
                
                switch (budget.operator_)
                {
                    case "dưới":
                        if (price <= budget.amount)
                        {
                            score += 2;
                            reasons.Add($"Phù hợp ngân sách dưới {budget.amount / 1000000}tr");
                        }
                        else
                        {
                            score -= 1;
                            reasons.Add($"Vượt ngân sách ({price / 1000000:F1}tr > {budget.amount / 1000000}tr)");
                        }
                        break;
                    case "trên":
                        if (price >= budget.amount)
                        {
                            score += 1;
                            reasons.Add($"Chất lượng cao trong tầm giá trên {budget.amount / 1000000}tr");
                        }
                        break;
                    default:
                        if (Math.Abs(price - budget.amount) <= budget.amount * 0.2m)
                        {
                            score += 1.5m;
                            reasons.Add($"Giá hợp lý quanh mức {budget.amount / 1000000}tr");
                        }
                        break;
                }
            }
            
            // Use case analysis
            if (requirements.ContainsKey("useCase"))
            {
                var useCase = requirements["useCase"].ToString();
                var productName = product.Name.ToLower();
                
                switch (useCase)
                {
                    case "gaming":
                        if (productName.Contains("gaming") || productName.Contains("rog") || productName.Contains("legion"))
                        {
                            score += 3;
                            reasons.Add("Chuyên gaming với hiệu năng cao");
                        }
                        else if (productName.Contains("workstation") || productName.Contains("creator"))
                        {
                            score += 2;
                            reasons.Add("Hiệu năng mạnh, có thể chơi game");
                        }
                        break;
                    case "study":
                    case "office":
                        if (productName.Contains("business") || productName.Contains("thinkpad") || productName.Contains("inspiron"))
                        {
                            score += 2;
                            reasons.Add("Thiết kế cho học tập/văn phòng");
                        }
                        break;
                    case "design":
                        if (productName.Contains("creator") || productName.Contains("studio") || productName.Contains("workstation"))
                        {
                            score += 3;
                            reasons.Add("Chuyên thiết kế đồ họa");
                        }
                        break;
                }
            }
            
            // Physical requirements
            if (requirements.ContainsKey("portable") && (bool)requirements["portable"])
            {
                if (product.Name.ToLower().Contains("ultrabook") || product.Name.ToLower().Contains("slim"))
                {
                    score += 2;
                    reasons.Add("Thiết kế mỏng nhẹ, dễ mang theo");
                }
            }
            
            // Brand preference
            if (requirements.ContainsKey("preferredBrand"))
            {
                var preferredBrand = requirements["preferredBrand"].ToString();
                if (product.Brand.ToLower().Contains(preferredBrand))
                {
                    score += 1.5m;
                    reasons.Add($"Đúng thương hiệu yêu thích ({product.Brand})");
                }
            }
            
            // Relative scoring within the comparison list
            var avgPrice = allProducts.Average(p => p.GetEffectivePrice());
            if (product.GetEffectivePrice() < avgPrice)
            {
                score += 0.5m;
                reasons.Add("Giá tốt trong danh sách so sánh");
            }
            
            contextAnalysis = $"Điểm phù hợp: {score:F1}/10 dựa trên {reasons.Count} tiêu chí";
            
            return (Math.Min(score, 10), reasons, contextAnalysis);
        }

        private string GenerateComparisonMessage(dynamic bestProduct, List<object> allProducts, Dictionary<string, object> requirements)
        {
            var productCount = allProducts.Count;
            
            if (productCount == 1)
            {
                return $"✅ {bestProduct.Name} là sản phẩm duy nhất trong danh sách so sánh:";
            }
            
            dynamic firstProduct = allProducts[0];
            dynamic secondProduct = allProducts.Count > 1 ? allProducts[1] : null;
            decimal scoreDiff = firstProduct.Score - (secondProduct?.Score ?? 0);
            
            if (scoreDiff > 2)
            {
                return $"✅ Trong {productCount} sản phẩm đang so sánh, {bestProduct.Name} vượt trội rõ ràng với yêu cầu của bạn:";
            }
            else if (scoreDiff > 1)
            {
                return $"✅ Dựa trên {productCount} sản phẩm trong danh sách, {bestProduct.Name} phù hợp nhất với nhu cầu của bạn:";
            }
            else
            {
                return $"💡 Trong {productCount} sản phẩm hiện tại, {bestProduct.Name} có phần phù hợp hơn, tuy nhiên sự khác biệt không lớn:";
            }
        }

        // Private helper methods
        private List<int> GetCompareProductIds()
        {
            var sessionData = HttpContext.Session.GetString(COMPARE_SESSION_KEY);
            if (string.IsNullOrEmpty(sessionData))
                return new List<int>();

            try
            {
                return JsonSerializer.Deserialize<List<int>>(sessionData) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private void SetCompareProductIds(List<int> productIds)
        {
            var sessionData = JsonSerializer.Serialize(productIds);
            HttpContext.Session.SetString(COMPARE_SESSION_KEY, sessionData);
        }

        private string GetComparisonType(Product product)
        {
            var categoryName = product.SubSubcategory?.Subcategory?.Category?.Name?.ToLower();
            var subcategoryName = product.SubSubcategory?.Subcategory?.Name?.ToLower();
            var subsubcategoryName = product.SubSubcategory?.Name?.ToLower();

            // Define comparison types based on category hierarchy
            if (categoryName?.Contains("laptop") == true || subcategoryName?.Contains("laptop") == true)
                return "laptop";
            if (categoryName?.Contains("chuột") == true || subcategoryName?.Contains("chuột") == true)
                return "mouse";
            if (categoryName?.Contains("bàn phím") == true || subcategoryName?.Contains("bàn phím") == true)
                return "keyboard";
            if (categoryName?.Contains("tai nghe") == true || subcategoryName?.Contains("tai nghe") == true)
                return "headphone";
            if (categoryName?.Contains("màn hình") == true || subcategoryName?.Contains("màn hình") == true)
                return "monitor";
            // PC Gaming detection
            if (categoryName?.Contains("pc") == true || subcategoryName?.Contains("pc") == true || 
                categoryName?.Contains("gaming") == true || subcategoryName?.Contains("gaming") == true ||
                subsubcategoryName?.Contains("pc") == true || subsubcategoryName?.Contains("gaming") == true)
                return "pc gaming";

            return "general";
        }

        private bool AreProductsCompatible(Product product1, Product product2)
        {
            // Check if products have the same comparison type (e.g., both laptops, both mice, etc.)
            var comparisonType1 = GetComparisonType(product1);
            var comparisonType2 = GetComparisonType(product2);
            
            // Allow comparison if they have the same comparison type
            if (comparisonType1 == comparisonType2)
                return true;
                
            // Fallback: check if they are in the same main category
            var categoryId1 = product1.SubSubcategory?.Subcategory?.CategoryID;
            var categoryId2 = product2.SubSubcategory?.Subcategory?.CategoryID;
            
            return categoryId1 == categoryId2;
        }

        private Dictionary<string, List<ComparisonCriteria>> GetComparisonCriteria(string comparisonType)
        {
            var criteria = new Dictionary<string, List<ComparisonCriteria>>();

            switch (comparisonType.ToLower())
            {
                case "laptop":
                    criteria["Hiệu năng"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "cpu", DisplayName = "CPU", Weight = 0.20m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "gpu", DisplayName = "GPU", Weight = 0.20m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "ram", DisplayName = "RAM", Weight = 0.15m, MaxScore = 10, Unit = "GB" },
                        new ComparisonCriteria { Name = "storage", DisplayName = "Ổ cứng", Weight = 0.10m, MaxScore = 10, Unit = "GB" },
                        new ComparisonCriteria { Name = "cpu_frequency", DisplayName = "Tần số CPU", Weight = 0.05m, MaxScore = 10, Unit = "GHz" }
                    };
                    criteria["Màn hình"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "refresh_rate_hz", DisplayName = "Tần số quét", Weight = 0.08m, MaxScore = 10, Unit = "Hz" },
                        new ComparisonCriteria { Name = "resolution_class", DisplayName = "Độ phân giải", Weight = 0.07m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "panel_type", DisplayName = "Loại panel", Weight = 0.05m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "screen_size", DisplayName = "Kích thước màn hình", Weight = 0.03m, MaxScore = 10, Unit = "inch" }
                    };
                    criteria["Thiết kế"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "weight", DisplayName = "Trọng lượng", Weight = 0.05m, MaxScore = 10, Unit = "kg" },
                        new ComparisonCriteria { Name = "battery_wh", DisplayName = "Pin", Weight = 0.04m, MaxScore = 10, Unit = "Wh" }
                    };
                    break;

                case "mouse":
                    criteria["Hiệu năng"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "dpi", DisplayName = "DPI", Weight = 0.30m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "polling_rate", DisplayName = "Tần số quét", Weight = 0.25m, MaxScore = 10, Unit = "Hz" },
                        new ComparisonCriteria { Name = "sensor", DisplayName = "Cảm biến", Weight = 0.20m, MaxScore = 10 }
                    };
                    criteria["Thiết kế"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "weight", DisplayName = "Trọng lượng", Weight = 0.15m, MaxScore = 10, Unit = "g" },
                        new ComparisonCriteria { Name = "ergonomics", DisplayName = "Thiết kế ergonomic", Weight = 0.10m, MaxScore = 10 }
                    };
                    break;

                case "keyboard":
                    criteria["Hiệu năng"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "switch_type", DisplayName = "Loại switch", Weight = 0.30m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "response_time", DisplayName = "Thời gian phản hồi", Weight = 0.25m, MaxScore = 10, Unit = "ms" },
                        new ComparisonCriteria { Name = "durability", DisplayName = "Độ bền", Weight = 0.20m, MaxScore = 10 }
                    };
                    criteria["Tính năng"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "backlight", DisplayName = "Đèn nền", Weight = 0.15m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "layout", DisplayName = "Layout", Weight = 0.10m, MaxScore = 10 }
                    };
                    break;

                case "pc gaming":
                case "pc":
                    criteria["Hiệu năng"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "cpu", DisplayName = "CPU", Weight = 0.25m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "vga", DisplayName = "VGA", Weight = 0.30m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "ram", DisplayName = "RAM", Weight = 0.20m, MaxScore = 10, Unit = "GB" }
                    };
                    criteria["Linh kiện"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "mainboard", DisplayName = "Mainboard", Weight = 0.10m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "storage", DisplayName = "Ổ cứng", Weight = 0.08m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "psu", DisplayName = "PSU", Weight = 0.05m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "case", DisplayName = "Case", Weight = 0.02m, MaxScore = 10 }
                    };
                    break;

                case "monitor":
                    criteria["Hiệu năng"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "refresh_rate_hz", DisplayName = "Tần số quét", Weight = 0.25m, MaxScore = 10, Unit = "Hz" },
                        new ComparisonCriteria { Name = "response_time", DisplayName = "Response time", Weight = 0.20m, MaxScore = 10, Unit = "ms" },
                        new ComparisonCriteria { Name = "resolution_class", DisplayName = "Độ phân giải", Weight = 0.20m, MaxScore = 10 }
                    };
                    criteria["Màn hình"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "panel_type", DisplayName = "Loại panel", Weight = 0.15m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "screen_size", DisplayName = "Kích thước", Weight = 0.10m, MaxScore = 10, Unit = "inch" },
                        new ComparisonCriteria { Name = "color_accuracy", DisplayName = "Độ chính xác màu", Weight = 0.08m, MaxScore = 10, Unit = "%" }
                    };
                    criteria["Kết nối"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "connectivity", DisplayName = "Cổng kết nối", Weight = 0.02m, MaxScore = 10 }
                    };
                    break;

                case "headphone":
                    criteria["audio"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "frequency_response", DisplayName = "Dải tần", Weight = 0.25m, MaxScore = 10, Unit = "Hz" },
                        new ComparisonCriteria { Name = "impedance", DisplayName = "Trở kháng", Weight = 0.20m, MaxScore = 10, Unit = "Ω" },
                        new ComparisonCriteria { Name = "noise_canceling", DisplayName = "Khử tiếng ồn", Weight = 0.25m, MaxScore = 10 }
                    };
                    criteria["comfort"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "weight", DisplayName = "Trọng lượng", Weight = 0.15m, MaxScore = 10, Unit = "g" },
                        new ComparisonCriteria { Name = "battery_life", DisplayName = "Thời lượng pin", Weight = 0.15m, MaxScore = 10, Unit = "h" }
                    };
                    break;

                default:
                    criteria["Tổng quan"] = new List<ComparisonCriteria>
                    {
                        new ComparisonCriteria { Name = "price", DisplayName = "Giá", Weight = 0.30m, MaxScore = 10, Unit = "đ" },
                        new ComparisonCriteria { Name = "brand", DisplayName = "Thương hiệu", Weight = 0.20m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "rating", DisplayName = "Đánh giá", Weight = 0.25m, MaxScore = 10 },
                        new ComparisonCriteria { Name = "warranty", DisplayName = "Bảo hành", Weight = 0.25m, MaxScore = 10 }
                    };
                    break;
            }

            return criteria;
        }

        private async Task<ProductAnalysis> GenerateProductAnalysis(List<Product> products, string comparisonType)
        {
            var analysis = new ProductAnalysis();
            var criteriaDict = GetComparisonCriteria(comparisonType);
            var allCriteria = criteriaDict.SelectMany(c => c.Value).ToList();

            // Calculate detailed scores for each product
            var productDetailedScores = new Dictionary<int, Dictionary<string, decimal>>();
            var productScores = new Dictionary<int, decimal>();

            foreach (var product in products)
            {
                decimal totalScore = 0;
                var categoryScores = new Dictionary<string, decimal>();
                var productSpecs = GetProductSpecifications(product);

                // Calculate scores by category for detailed analysis
                foreach (var category in criteriaDict)
                {
                    decimal categoryScore = 0;
                    decimal categoryWeight = 0;

                    foreach (var criteria in category.Value)
                    {
                        var score = CalculateCriteriaScore(productSpecs, criteria, products);
                        categoryScore += score * criteria.Weight;
                        categoryWeight += criteria.Weight;
                        totalScore += score * criteria.Weight;
                    }

                    if (categoryWeight > 0)
                    {
                        categoryScores[category.Key] = Math.Round(categoryScore / categoryWeight, 1);
                    }
                }

                productDetailedScores[product.ProductID] = categoryScores;
                productScores[product.ProductID] = totalScore;
                analysis.OverallScores[product.Name] = Math.Round(totalScore, 1);
            }

            // Find recommended product with context-aware analysis
            var recommendedProduct = FindBestProductWithContext(products, productScores, productDetailedScores, comparisonType);
            analysis.RecommendedProductId = recommendedProduct.ProductID;
            analysis.RecommendedProductName = recommendedProduct.Name;
            analysis.RecommendedProductImage = recommendedProduct.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL 
                ?? recommendedProduct.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg";

            // Generate comprehensive analysis reasons
            analysis.Reasons = GenerateAdvancedAnalysisReasons(recommendedProduct, products, productDetailedScores, comparisonType);

            return analysis;
        }

        private Product FindBestProductWithContext(List<Product> products, Dictionary<int, decimal> scores, 
            Dictionary<int, Dictionary<string, decimal>> detailedScores, string comparisonType)
        {
            // Contextual weighting based on product type
            var contextWeights = GetContextualWeights(comparisonType);
            
            var contextualScores = new Dictionary<int, decimal>();
            
            foreach (var product in products)
            {
                decimal contextualScore = 0;
                var productCategoryScores = detailedScores[product.ProductID];
                
                foreach (var categoryScore in productCategoryScores)
                {
                    var weight = contextWeights.ContainsKey(categoryScore.Key) ? contextWeights[categoryScore.Key] : 1.0m;
                    contextualScore += categoryScore.Value * weight;
                }
                
                // Factor in brand reputation and price-performance ratio
                contextualScore = AdjustForBrandAndValue(product, contextualScore);
                
                contextualScores[product.ProductID] = contextualScore;
            }
            
            return products.OrderByDescending(p => contextualScores[p.ProductID]).First();
        }

        private Dictionary<string, decimal> GetContextualWeights(string comparisonType)
        {
            return comparisonType.ToLower() switch
            {
                "laptop" => new Dictionary<string, decimal>
                {
                    ["Hiệu năng"] = 1.3m,    // Performance is crucial for laptops
                    ["Thiết kế"] = 0.9m,      // Design matters but less than performance
                    ["Tổng quan"] = 1.1m      // Overall value consideration
                },
                "mouse" => new Dictionary<string, decimal>
                {
                    ["Hiệu năng"] = 1.4m,    // DPI, polling rate are critical
                    ["Thiết kế"] = 1.2m,      // Ergonomics very important for mouse
                    ["Tổng quan"] = 0.8m      // Price less critical for peripherals
                },
                "keyboard" => new Dictionary<string, decimal>
                {
                    ["Hiệu năng"] = 1.3m,    // Switch type, response time crucial
                    ["Tính năng"] = 1.1m,     // Features like RGB, layout important
                    ["Tổng quan"] = 0.9m
                },
                "pc gaming" => new Dictionary<string, decimal>
                {
                    ["Hiệu năng"] = 1.4m,    // CPU, VGA performance is critical
                    ["Linh kiện"] = 1.1m,     // Component quality matters
                    ["Tổng quan"] = 1.0m
                },
                "pc" => new Dictionary<string, decimal>
                {
                    ["Hiệu năng"] = 1.4m,    // CPU, VGA performance is critical
                    ["Linh kiện"] = 1.1m,     // Component quality matters
                    ["Tổng quan"] = 1.0m
                },
                "monitor" => new Dictionary<string, decimal>
                {
                    ["Hiệu năng"] = 1.4m,     // Refresh rate, response time critical for gaming
                    ["Màn hình"] = 1.2m,      // Panel type, size important for productivity
                    ["Kết nối"] = 0.8m,       // Connectivity less critical
                    ["Tổng quan"] = 1.0m
                },
                "headphone" => new Dictionary<string, decimal>
                {
                    ["audio"] = 1.5m,         // Audio quality most important
                    ["comfort"] = 1.2m,       // Comfort crucial for long use
                    ["Tổng quan"] = 0.8m
                },
                _ => new Dictionary<string, decimal>
                {
                    ["Tổng quan"] = 1.0m
                }
            };
        }

        private decimal AdjustForBrandAndValue(Product product, decimal baseScore)
        {
            var brandBonus = GetBrandReputationBonus(product.Brand);
            var valueRatio = CalculateValueRatio(product);
            
            // Apply brand bonus (max 10% boost)
            baseScore *= (1 + brandBonus * 0.1m);
            
            // Apply value ratio (max 15% adjustment)
            baseScore *= (1 + valueRatio * 0.15m);
            
            return baseScore;
        }

        private decimal GetBrandReputationBonus(string brand)
        {
            var brandScores = new Dictionary<string, decimal>
            {
                ["Apple"] = 1.0m, ["Dell"] = 0.9m, ["HP"] = 0.8m, ["Lenovo"] = 0.8m,
                ["Asus"] = 0.9m, ["MSI"] = 0.8m, ["Acer"] = 0.6m, ["Gigabyte"] = 0.7m,
                ["Razer"] = 0.9m, ["Logitech"] = 0.9m, ["Corsair"] = 0.8m, ["SteelSeries"] = 0.7m,
                ["Sony"] = 0.9m, ["Bose"] = 1.0m, ["Sennheiser"] = 1.0m, ["Audio-Technica"] = 0.8m
            };
            
            return brandScores.ContainsKey(brand) ? brandScores[brand] : 0.5m;
        }

        private decimal CalculateValueRatio(Product product)
        {
            // Simple value calculation: lower price for same features = higher value
            var price = product.GetEffectivePrice();
            var avgPrice = 15000000; // Assume average product price
            
            if (price < avgPrice * 0.7m) return 0.3m;      // Great value
            if (price < avgPrice) return 0.1m;             // Good value  
            if (price > avgPrice * 1.5m) return -0.2m;     // Expensive
            
            return 0m; // Average value
        }

        private Dictionary<string, object> GetProductSpecifications(Product product)
        {
            var specs = new Dictionary<string, object>();

            // Add basic product info
            specs["price"] = product.GetEffectivePrice();
            specs["brand"] = product.Brand ?? "";
            specs["rating"] = product.Reviews?.Any() == true ? product.Reviews.Average(r => r.Rating) : 0;
            specs["name"] = product.Name ?? "";

            // Add comprehensive product attributes with smart mapping
            foreach (var attr in product.ProductAttributeValues)
            {
                var attributeName = attr.AttributeValue.ProductAttribute.AttributeName.ToLower().Trim();
                var valueName = attr.AttributeValue.ValueName?.Trim() ?? "";

                // Map common attribute names to standardized keys
                var mappedKey = MapAttributeNameToKey(attributeName);
                
                // Also try direct mapping with normalized key
                if (string.IsNullOrEmpty(mappedKey))
                {
                    mappedKey = attributeName.Replace(" ", "_").Replace("-", "_");
                }
                
                if (!string.IsNullOrEmpty(mappedKey) && !string.IsNullOrEmpty(valueName))
                {
                    specs[mappedKey] = valueName;
                }
            }

            // Extract additional technical specs for detailed comparison
            ExtractTechnicalSpecs(specs, product);

            return specs;
        }

        private string MapAttributeNameToKey(string attributeName)
        {
            var mappings = new Dictionary<string, string>
            {
                // CPU & Performance - Map exact database attribute names
                ["cpu"] = "cpu",
                ["vi xử lý"] = "cpu",
                ["processor"] = "cpu",
                ["chipset"] = "chipset",
                
                // Memory & Storage - Map exact database attribute names
                ["ram"] = "ram",
                ["bộ nhớ"] = "ram",
                ["memory"] = "ram",
                ["storage"] = "ổ_cứng",
                ["ổ cứng"] = "ổ_cứng",
                ["ổ_cứng"] = "ổ_cứng",
                ["hard drive"] = "ổ_cứng",
                ["ssd"] = "ổ_cứng",
                ["hdd"] = "ổ_cứng",
                
                // Graphics - Map exact database attribute names
                ["gpu"] = "vga",
                ["card đồ họa"] = "vga", 
                ["card_đồ_họa"] = "vga",
                ["graphics card"] = "vga",
                ["vga"] = "vga",
                ["vram"] = "vga",
                
                // Display specs - Map exact database attribute names
                ["màn hình"] = "màn_hình",
                ["màn_hình"] = "màn_hình", 
                ["screen"] = "màn_hình",
                ["display"] = "màn_hình",
                ["screen size"] = "screen_size",
                ["kích thước màn hình"] = "screen_size",
                ["panel type"] = "panel_type",
                ["loại panel"] = "panel_type",
                ["resolution"] = "resolution_class",
                ["độ phân giải"] = "độ_phân_giải",    // AttributeName: "Độ phân giải" (từ monitor SQL)
                ["kích thước"] = "kích_thước",        // AttributeName: "Kích thước" (từ monitor SQL)
                ["size"] = "kích_thước",
                ["screen size"] = "kích_thước",
                
                // Monitor specific specs - Map exact database attribute names (từ insert_monitor_products.sql)
                ["tần số quét"] = "tần_số_quét",      // AttributeName: "Tần số quét"
                ["refresh rate"] = "tần_số_quét",
                ["tấm nền"] = "tấm_nền",              // AttributeName: "Tấm nền"
                ["panel type"] = "tấm_nền",
                ["loại panel"] = "tấm_nền",
                ["thời gian phản hồi"] = "thời_gian_phản_hồi",  // AttributeName: "Thời gian phản hồi"
                ["response time"] = "thời_gian_phản_hồi",
                ["công nghệ đồng bộ"] = "công_nghệ_đồng_bộ",    // AttributeName: "Công nghệ đồng bộ"
                ["sync technology"] = "công_nghệ_đồng_bộ",
                ["cổng kết nối"] = "cổng_kết_nối",    // AttributeName: "Cổng kết nối"
                ["ports"] = "cổng_kết_nối",
                ["screen size"] = "màn_hình",
                ["inch"] = "màn_hình",
                ["độ phân giải"] = "màn_hình",
                ["resolution"] = "màn_hình",
                ["tần số quét"] = "tần_số_quét",
                ["tần_số_quét"] = "tần_số_quét",
                ["refresh rate"] = "tần_số_quét",
                ["hz"] = "tần_số_quét",
                ["tấm nền"] = "panel_type",
                ["panel"] = "panel_type",
                ["ips"] = "panel_type",
                ["va"] = "panel_type",
                ["tn"] = "panel_type",
                ["oled"] = "panel_type",
                ["thời gian phản hồi"] = "response_time",
                ["response time"] = "response_time",
                ["công nghệ đồng bộ"] = "sync_technology",
                ["freesync"] = "sync_technology",
                ["g-sync"] = "sync_technology",
                ["cổng kết nối"] = "connection_ports",
                ["ports"] = "connection_ports",
                ["port"] = "connection_ports",
                ["kết nối"] = "connection_ports",
                ["connectivity"] = "connection_ports",
                ["hdmi"] = "connection_ports",
                ["displayport"] = "connection_ports",
                ["usb"] = "connection_ports",
                ["màu sắc"] = "color_gamut",
                ["color gamut"] = "color_gamut",
                ["độ sáng"] = "brightness",
                ["brightness"] = "brightness",
                ["nits"] = "brightness",
                ["cd/m2"] = "brightness",
                ["cd/m²"] = "brightness",
                ["tỉ lệ tương phản"] = "contrast_ratio",
                ["contrast ratio"] = "contrast_ratio",
                ["contrast"] = "contrast_ratio",
                ["tương phản"] = "contrast_ratio",
                
                // Mouse specs - Map exact database attribute names (từ insert_chuot_products.sql)
                ["dpi"] = "dpi",
                ["cpi"] = "dpi",
                ["polling rate"] = "polling_rate",
                ["tần số polling"] = "polling_rate",
                ["cảm biến"] = "cảm_biến",  // AttributeName: "Cảm biến"
                ["sensor"] = "cảm_biến",
                ["kết nối"] = "kết_nối",    // AttributeName: "Kết nối" 
                ["connection"] = "kết_nối",
                ["connectivity"] = "kết_nối",
                ["số nút"] = "số_nút",      // AttributeName: "Số nút"
                ["buttons"] = "số_nút",
                ["led"] = "led",            // AttributeName: "LED"
                ["thời lượng pin"] = "thời_lượng_pin",  // AttributeName: "Thời lượng pin"
                ["battery life"] = "thời_lượng_pin",
                ["battery"] = "thời_lượng_pin",
                ["trọng lượng"] = "trọng_lượng",  // AttributeName: "Trọng lượng"
                ["weight"] = "trọng_lượng",
                
                // Keyboard specs - Map exact database attribute names (từ insert_keyboard_products.sql)
                ["switch"] = "switch",         // AttributeName: "Switch"
                ["loại switch"] = "switch",
                ["key switch"] = "switch",
                ["switch type"] = "switch",
                ["layout"] = "layout",         // AttributeName: "Layout"
                ["bố cục"] = "layout",
                ["kiểu bàn phím"] = "kiểu_bàn_phím",  // AttributeName: "Kiểu bàn phím"
                ["keyboard type"] = "kiểu_bàn_phím",
                ["màu sắc"] = "màu_sắc",       // AttributeName: "Màu sắc"
                ["color"] = "màu_sắc",
                ["pin"] = "pin",               // AttributeName: "Pin"
                ["battery"] = "pin",
                ["kết nối"] = "kết_nối",       // AttributeName: "Kết nối" (cũng có trong keyboard)
                
                // Audio specs
                ["driver"] = "driver_size",
                ["frequency response"] = "frequency_response",
                ["tần số"] = "frequency_response",
                ["impedance"] = "impedance",
                ["trở kháng"] = "impedance",
                ["noise cancelling"] = "noise_canceling",
                ["chống ồn"] = "noise_canceling",
                
                // PC Gaming specs - Map exact database attribute names
                ["vga"] = "vga",
                ["graphics"] = "vga",
                ["mainboard"] = "mainboard",
                ["motherboard"] = "mainboard",
                ["psu"] = "psu",
                ["power supply"] = "psu",
                ["case"] = "case",
                ["chassis"] = "case",
                
                // Connectivity
                ["bluetooth"] = "bluetooth",
                ["wifi"] = "wifi",
                ["usb"] = "usb_ports",
                ["cổng kết nối"] = "ports",
                ["ports"] = "ports",
                ["wireless"] = "wireless",
                ["không dây"] = "wireless",
                
                // System & OS - Map exact database attribute names
                ["hệ điều hành"] = "hệ_điều_hành",
                ["hệ_điều_hành"] = "hệ_điều_hành",
                ["operating system"] = "hệ_điều_hành",
                ["os"] = "hệ_điều_hành",
                ["windows"] = "hệ_điều_hành",
                
                // Physical specs - Map exact database attribute names
                ["trọng lượng"] = "weight",
                ["trọng_lượng"] = "weight",
                ["weight"] = "weight",
                ["kg"] = "weight",
                ["g"] = "weight",
                
                // Battery & Power
                ["pin"] = "battery",
                ["battery"] = "battery",
                ["wh"] = "battery_capacity",
                ["mah"] = "battery_capacity",
                ["adapter"] = "power_adapter",
                
                // Design & Build
                ["màu sắc"] = "color",
                ["color"] = "color",
                ["chất liệu"] = "material",
                ["material"] = "material",
                ["kích thước"] = "dimensions",
                ["dimensions"] = "dimensions",
                ["size"] = "dimensions",
                
                // Features
                ["rgb"] = "rgb",
                ["backlight"] = "backlight",
                ["đèn nền"] = "backlight",
                ["waterproof"] = "waterproof",
                ["chống nước"] = "waterproof",
                ["warranty"] = "warranty",
                ["bảo hành"] = "warranty"
            };

            foreach (var mapping in mappings)
            {
                if (attributeName.Contains(mapping.Key))
                {
                    return mapping.Value;
                }
            }

            // If no mapping found, use sanitized attribute name
            return attributeName.Replace(" ", "_").Replace("-", "_");
        }

        private void ExtractTechnicalSpecs(Dictionary<string, object> specs, Product product)
        {
            // Extract numerical values from complex specs
                            foreach (var key in specs.Keys.ToList())
                {
                    var value = specs[key].ToString();
                    
                    switch (key)
                    {
                        case "display":
                        case "resolution":
                        case "screen_size":
                        case "kích_thước":
                            ExtractDisplaySpecs(specs, value);
                            break;
                        
                    case "refresh_rate":
                        var refreshRate = ExtractNumericValue(value);
                        if (refreshRate > 0)
                        {
                            specs["refresh_rate_hz"] = refreshRate;
                        }
                        break;
                        
                    case "cpu":
                        ExtractCPUSpecs(specs, value);
                        break;
                        
                    case "gpu":
                        ExtractGPUSpecs(specs, value);
                        break;
                        
                    case "battery":
                        var batteryCapacity = ExtractNumericValue(value);
                        if (batteryCapacity > 0)
                        {
                            specs["battery_wh"] = batteryCapacity;
                        }
                        break;
                        
                    case "response_time":
                        var responseTime = ExtractNumericValue(value);
                        if (responseTime > 0)
                        {
                            specs["response_time"] = responseTime;
                        }
                        break;
                        
                    case "sync_technology":
                    case "connection_ports":
                        // Keep original string value for sync tech and ports
                        break;
                        
                    case "brightness":
                        var brightnessValue = ExtractNumericValue(value);
                        if (brightnessValue > 0)
                        {
                            specs["brightness"] = brightnessValue;
                        }
                        break;
                }
            }
        }

        private void ExtractDisplaySpecs(Dictionary<string, object> specs, string displayInfo)
        {
            var displayLower = displayInfo.ToLower();
            
            // Extract resolution
            var resolutionPatterns = new[]
            {
                @"(\d{3,4})\s*[x×]\s*(\d{3,4})",
                @"(4k|uhd|2160p)",
                @"(2k|1440p|qhd)",
                @"(1080p|fhd|full hd)",
                @"(720p|hd)"
            };
            
            foreach (var pattern in resolutionPatterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(displayLower, pattern);
                if (match.Success)
                {
                    if (match.Groups[1].Value.Contains("4k") || match.Groups[1].Value.Contains("2160"))
                        specs["resolution_class"] = "4K";
                    else if (match.Groups[1].Value.Contains("2k") || match.Groups[1].Value.Contains("1440"))
                        specs["resolution_class"] = "2K";
                    else if (match.Groups[1].Value.Contains("1080"))
                        specs["resolution_class"] = "FHD";
                    else if (match.Groups[1].Value.Contains("720"))
                        specs["resolution_class"] = "HD";
                    break;
                }
            }
            
            // Extract refresh rate
            var refreshMatch = System.Text.RegularExpressions.Regex.Match(displayInfo, @"(\d{2,3})\s*hz", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (refreshMatch.Success && double.TryParse(refreshMatch.Groups[1].Value, out double refreshRate))
            {
                specs["refresh_rate_hz"] = refreshRate;
            }
            
            // Extract panel type
            if (displayLower.Contains("ips")) specs["panel_type"] = "IPS";
            else if (displayLower.Contains("va")) specs["panel_type"] = "VA";
            else if (displayLower.Contains("tn")) specs["panel_type"] = "TN";
            else if (displayLower.Contains("oled")) specs["panel_type"] = "OLED";
            else if (displayLower.Contains("amoled")) specs["panel_type"] = "AMOLED";
            
            // Extract screen size - multiple patterns
            var sizePatterns = new[]
            {
                @"(\d{1,2}\.?\d?)\s*inch",
                @"(\d{1,2}\.?\d?)\s*""",
                @"(\d{1,2}\.?\d?)''"
            };
            
            foreach (var pattern in sizePatterns)
            {
                var sizeMatch = System.Text.RegularExpressions.Regex.Match(displayInfo, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (sizeMatch.Success && double.TryParse(sizeMatch.Groups[1].Value, out double screenSize))
                {
                    specs["screen_size"] = screenSize;
                    break;
                }
            }
        }

        private void ExtractCPUSpecs(Dictionary<string, object> specs, string cpuInfo)
        {
            var cpuLower = cpuInfo.ToLower();
            
            // Extract CPU generation and series
            if (cpuLower.Contains("intel"))
            {
                specs["cpu_brand"] = "Intel";
                
                var intelMatch = System.Text.RegularExpressions.Regex.Match(cpuInfo, @"i([3579])-?(\d{4,5})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (intelMatch.Success)
                {
                    specs["cpu_series"] = $"Core i{intelMatch.Groups[1].Value}";
                    specs["cpu_model"] = intelMatch.Groups[2].Value;
                }
            }
            else if (cpuLower.Contains("amd") || cpuLower.Contains("ryzen"))
            {
                specs["cpu_brand"] = "AMD";
                
                var ryzenMatch = System.Text.RegularExpressions.Regex.Match(cpuInfo, @"ryzen\s*([3579])", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (ryzenMatch.Success)
                {
                    specs["cpu_series"] = $"Ryzen {ryzenMatch.Groups[1].Value}";
                }
            }
            
            // Extract frequency
            var freqMatch = System.Text.RegularExpressions.Regex.Match(cpuInfo, @"(\d\.?\d*)\s*ghz", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (freqMatch.Success && double.TryParse(freqMatch.Groups[1].Value, out double frequency))
            {
                specs["cpu_frequency"] = frequency;
            }
        }

        private void ExtractGPUSpecs(Dictionary<string, object> specs, string gpuInfo)
        {
            var gpuLower = gpuInfo.ToLower();
            
            if (gpuLower.Contains("nvidia") || gpuLower.Contains("geforce"))
            {
                specs["gpu_brand"] = "NVIDIA";
                
                var rtxMatch = System.Text.RegularExpressions.Regex.Match(gpuInfo, @"rtx\s*(\d{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (rtxMatch.Success)
                {
                    specs["gpu_series"] = "RTX";
                    specs["gpu_model"] = rtxMatch.Groups[1].Value;
                }
                
                var gtxMatch = System.Text.RegularExpressions.Regex.Match(gpuInfo, @"gtx\s*(\d{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (gtxMatch.Success)
                {
                    specs["gpu_series"] = "GTX";
                    specs["gpu_model"] = gtxMatch.Groups[1].Value;
                }
            }
            else if (gpuLower.Contains("amd") || gpuLower.Contains("radeon"))
            {
                specs["gpu_brand"] = "AMD";
                
                var rxMatch = System.Text.RegularExpressions.Regex.Match(gpuInfo, @"rx\s*(\d{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (rxMatch.Success)
                {
                    specs["gpu_series"] = "RX";
                    specs["gpu_model"] = rxMatch.Groups[1].Value;
                }
            }
        }

        private decimal CalculateCriteriaScore(Dictionary<string, object> specs, ComparisonCriteria criteria, List<Product> allProducts)
        {
            if (!specs.ContainsKey(criteria.Name))
                return 5; // Default mid-range score

            var value = specs[criteria.Name];

            switch (criteria.Name.ToLower())
            {
                case "price":
                    // Lower price gets higher score (inverted)
                    var prices = allProducts.Select(p => p.GetEffectivePrice()).ToList();
                    var price = Convert.ToDecimal(value);
                    var maxPrice = prices.Max();
                    var minPrice = prices.Min();
                    if (maxPrice == minPrice) return criteria.MaxScore;
                    
                    // Normalize and invert (lower price = higher score)
                    var normalizedScore = 1 - ((price - minPrice) / (maxPrice - minPrice));
                    return normalizedScore * criteria.MaxScore;

                case "rating":
                    var rating = Convert.ToDecimal(value);
                    return (rating / 5) * criteria.MaxScore;

                case "ram":
                case "storage":
                case "vram":
                    // Extract numeric value for RAM/Storage (e.g., "16GB" -> 16)
                    var numericValue = ExtractNumericValue(value.ToString());
                    var allNumericValues = allProducts.SelectMany(p => 
                        p.ProductAttributeValues.Where(av => av.AttributeValue.ProductAttribute.AttributeName.ToLower().Contains(criteria.Name))
                        .Select(av => ExtractNumericValue(av.AttributeValue.ValueName))
                    ).Where(v => v > 0).ToList();
                    
                    if (allNumericValues.Any())
                    {
                        var maxValue = allNumericValues.Max();
                        var minValue = allNumericValues.Min();
                        if (maxValue == minValue) return criteria.MaxScore;
                        
                        var normalizedValue = (numericValue - minValue) / (maxValue - minValue);
                        return (decimal)normalizedValue * criteria.MaxScore;
                    }
                    return 7;

                case "cpu_score":
                case "gpu_score":
                case "dpi":
                case "polling_rate":
                case "frequency_response":
                case "refresh_rate_hz":
                case "cpu_frequency":
                case "battery_wh":
                    // Higher is better for performance metrics
                    var performanceValue = ExtractNumericValue(value.ToString());
                    var allPerformanceValues = allProducts.SelectMany(p =>
                        {
                            var productSpecs = GetProductSpecifications(p);
                            return productSpecs.ContainsKey(criteria.Name) 
                                ? new[] { ExtractNumericValue(productSpecs[criteria.Name].ToString()) }
                                : new double[0];
                        }
                    ).Where(v => v > 0).ToList();
                    
                    if (allPerformanceValues.Any())
                    {
                        var maxPerf = allPerformanceValues.Max();
                        var minPerf = allPerformanceValues.Min();
                        if (maxPerf == minPerf) return criteria.MaxScore;
                        
                        var normalizedPerf = (performanceValue - minPerf) / (maxPerf - minPerf);
                        return (decimal)normalizedPerf * criteria.MaxScore;
                    }
                    return 7;

                case "resolution_class":
                    // Resolution quality scoring
                    var resolutionValue = value.ToString().ToUpper();
                    var resolutionScores = new Dictionary<string, decimal>
                    {
                        ["4K"] = 10, ["UHD"] = 10, ["2160P"] = 10,
                        ["2K"] = 8.5m, ["QHD"] = 8.5m, ["1440P"] = 8.5m,
                        ["FHD"] = 7, ["1080P"] = 7, ["FULL HD"] = 7,
                        ["HD"] = 5, ["720P"] = 5
                    };
                    return resolutionScores.ContainsKey(resolutionValue) ? resolutionScores[resolutionValue] : 6;

                case "panel_type":
                    // Panel type quality scoring
                    var panelValue = value.ToString().ToUpper();
                    var panelScores = new Dictionary<string, decimal>
                    {
                        ["OLED"] = 10, ["AMOLED"] = 10,
                        ["IPS"] = 9,
                        ["VA"] = 7.5m,
                        ["TN"] = 6
                    };
                    return panelScores.ContainsKey(panelValue) ? panelScores[panelValue] : 7;

                case "brand":
                    // Brand scoring based on reputation
                    var brandValue = value.ToString().ToLower();
                    var brandScores = new Dictionary<string, decimal>
                    {
                        ["apple"] = 10, ["dell"] = 9, ["hp"] = 8.5m, ["lenovo"] = 8.5m,
                        ["asus"] = 9, ["msi"] = 8.5m, ["acer"] = 7.5m, ["gigabyte"] = 8,
                        ["razer"] = 9, ["logitech"] = 9, ["corsair"] = 8.5m, ["steelseries"] = 8,
                        ["sony"] = 9, ["bose"] = 9.5m, ["sennheiser"] = 9.5m, ["audio-technica"] = 8.5m
                    };
                    return brandScores.ContainsKey(brandValue) ? brandScores[brandValue] : 6;

                case "switch_type":
                    // Mechanical switches scoring
                    var switchValue = value.ToString().ToLower();
                    if (switchValue.Contains("mechanical") || switchValue.Contains("cherry mx") || 
                        switchValue.Contains("optical") || switchValue.Contains("kailh"))
                        return 9;
                    else if (switchValue.Contains("membrane") || switchValue.Contains("rubber dome"))
                        return 5;
                    return 7;

                case "noise_canceling":
                case "wireless":
                case "rgb":
                case "backlight":
                    // Boolean features
                    var boolValue = value.ToString().ToLower();
                    if (boolValue.Contains("yes") || boolValue.Contains("có") || boolValue.Contains("active") ||
                        boolValue.Contains("true") || boolValue == "1")
                        return criteria.MaxScore;
                    return criteria.MaxScore * 0.3m;

                default:
                    // For string comparisons, give random scores between 6-8
                    return 6 + (decimal)(new Random().NextDouble() * 2);
            }
        }

        private double ExtractNumericValue(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0;
            
            var inputLower = input.ToLower().Trim();
            
            // Remove common units and extract numbers
            var cleanInput = inputLower
                .Replace("gb", "").Replace("tb", "").Replace("hz", "")
                .Replace("dpi", "").Replace("ms", "").Replace("g", "")
                .Replace("ω", "").Replace("h", "").Replace(",", "")
                .Replace("inch", "").Replace("\"", "").Replace("nits", "")
                .Replace("cd/m²", "").Replace("cd/m2", "")
                .Trim();
            
            // Handle TB to GB conversion
            if (inputLower.Contains("tb"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(inputLower, @"(\d+\.?\d*)\s*tb");
                if (match.Success && double.TryParse(match.Groups[1].Value, out double tbValue))
                    return tbValue * 1024; // Convert TB to GB
            }
            
            // Handle KHz to Hz conversion
            if (inputLower.Contains("khz"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(inputLower, @"(\d+\.?\d*)\s*khz");
                if (match.Success && double.TryParse(match.Groups[1].Value, out double khzValue))
                    return khzValue * 1000; // Convert KHz to Hz
            }
            
            // Handle resolution strings (e.g., "Full HD (1920 x 1080)" -> 1920)
            if (inputLower.Contains("x") && (inputLower.Contains("1920") || inputLower.Contains("2560") || inputLower.Contains("3840")))
            {
                var resMatch = System.Text.RegularExpressions.Regex.Match(inputLower, @"(\d{3,4})\s*[x×]\s*(\d{3,4})");
                if (resMatch.Success && double.TryParse(resMatch.Groups[1].Value, out double width))
                    return width; // Return width as resolution metric
            }
            
            // Special handling for common resolution names
            if (inputLower.Contains("4k") || inputLower.Contains("uhd") || inputLower.Contains("2160p"))
                return 3840; // 4K width
            if (inputLower.Contains("2k") || inputLower.Contains("qhd") || inputLower.Contains("1440p"))
                return 2560; // 2K width
            if (inputLower.Contains("fhd") || inputLower.Contains("full hd") || inputLower.Contains("1080p"))
                return 1920; // FHD width
            if (inputLower.Contains("hd") || inputLower.Contains("720p"))
                return 1280; // HD width
            
            // Extract first number found
            var numMatch = System.Text.RegularExpressions.Regex.Match(cleanInput, @"[\d\.]+");
            if (numMatch.Success && double.TryParse(numMatch.Value, out double result))
                return result;
            
            return 0;
        }

        private List<string> GenerateAdvancedAnalysisReasons(Product recommendedProduct, List<Product> allProducts, 
            Dictionary<int, Dictionary<string, decimal>> detailedScores, string comparisonType)
        {
            var reasons = new List<string>();
            var productSpecs = GetProductSpecifications(recommendedProduct);
            var allProductSpecs = allProducts.ToDictionary(p => p.ProductID, p => GetProductSpecifications(p));

            // 1. Direct specification comparisons with concrete numbers
            var specComparisons = GenerateSpecificComparisons(recommendedProduct, allProducts, allProductSpecs, comparisonType);
            reasons.AddRange(specComparisons);

            // 2. Performance analysis with real-world implications
            var performanceAnalysis = GeneratePerformanceAnalysis(recommendedProduct, allProducts, productSpecs, comparisonType);
            reasons.AddRange(performanceAnalysis);

            // 3. Competitive positioning with exact metrics
            var competitivePosition = GenerateCompetitivePositioning(recommendedProduct, allProducts, allProductSpecs, comparisonType);
            reasons.AddRange(competitivePosition);

            // 4. Price-performance ratio with detailed breakdown
            var valueAnalysis = GenerateDetailedValueAnalysis(recommendedProduct, allProducts, productSpecs);
            reasons.AddRange(valueAnalysis);

            // 5. Technical advantages with specific benefits
            var technicalAdvantages = GenerateTechnicalAdvantages(recommendedProduct, allProducts, productSpecs, comparisonType);
            reasons.AddRange(technicalAdvantages);

            // 6. User experience insights with practical examples
            var userExperience = GenerateUserExperienceInsights(recommendedProduct, productSpecs, comparisonType);
            reasons.AddRange(userExperience);

            // 7. Brand and ecosystem benefits
            var brandAdvantages = GenerateBrandEcosystemAdvantages(recommendedProduct, allProducts);
            reasons.AddRange(brandAdvantages);

            // 8. Purchase decision factors
            var purchaseFactors = GeneratePurchaseDecisionFactors(recommendedProduct, allProducts);
            reasons.AddRange(purchaseFactors);

            return reasons.Take(8).ToList();
        }

        private List<string> GenerateSpecificComparisons(Product recommendedProduct, List<Product> allProducts, 
            Dictionary<int, Dictionary<string, object>> allSpecs, string comparisonType)
        {
            var comparisons = new List<string>();
            var productSpecs = allSpecs[recommendedProduct.ProductID];

            switch (comparisonType.ToLower())
            {
                case "laptop":
                    // Display Comparison - Priority on gaming specs
                    if (productSpecs.ContainsKey("refresh_rate_hz"))
                    {
                        var refreshComparison = CompareRefreshRateSpecs(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(refreshComparison)) comparisons.Add(refreshComparison);
                    }

                    // CPU Comparison
                    if (productSpecs.ContainsKey("cpu"))
                    {
                        var cpuComparison = CompareCPUSpecs(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(cpuComparison)) comparisons.Add(cpuComparison);
                    }

                    // Display Quality Comparison
                    if (productSpecs.ContainsKey("resolution_class") || productSpecs.ContainsKey("panel_type"))
                    {
                        var displayComparison = CompareDisplayQuality(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(displayComparison)) comparisons.Add(displayComparison);
                    }

                    // RAM Comparison
                    if (productSpecs.ContainsKey("ram"))
                    {
                        var ramComparison = CompareRAMSpecs(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(ramComparison)) comparisons.Add(ramComparison);
                    }

                    // Storage Comparison
                    if (productSpecs.ContainsKey("storage"))
                    {
                        var storageComparison = CompareStorageSpecs(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(storageComparison)) comparisons.Add(storageComparison);
                    }
                    break;

                case "mouse":
                    // DPI Comparison
                    if (productSpecs.ContainsKey("dpi"))
                    {
                        var dpiComparison = CompareDPISpecs(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(dpiComparison)) comparisons.Add(dpiComparison);
                    }

                    // Polling Rate Comparison
                    if (productSpecs.ContainsKey("polling_rate"))
                    {
                        var pollingComparison = ComparePollingRateSpecs(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(pollingComparison)) comparisons.Add(pollingComparison);
                    }

                    // Sensor Analysis
                    if (productSpecs.ContainsKey("cảm_biến"))
                    {
                        var sensorAnalysis = AnalyzeMouseSensor(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(sensorAnalysis)) comparisons.Add(sensorAnalysis);
                    }

                    // Weight Analysis
                    if (productSpecs.ContainsKey("trọng_lượng"))
                    {
                        var weightAnalysis = AnalyzeMouseWeight(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(weightAnalysis)) comparisons.Add(weightAnalysis);
                    }

                    // Connectivity Analysis
                    if (productSpecs.ContainsKey("kết_nối"))
                    {
                        var connectivityAnalysis = AnalyzeMouseConnectivity(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(connectivityAnalysis)) comparisons.Add(connectivityAnalysis);
                    }

                    // Battery Analysis
                    if (productSpecs.ContainsKey("thời_lượng_pin"))
                    {
                        var batteryAnalysis = AnalyzeMouseBattery(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(batteryAnalysis)) comparisons.Add(batteryAnalysis);
                    }
                    break;

                case "keyboard":
                    // Switch Type Analysis
                    if (productSpecs.ContainsKey("switch"))
                    {
                        var switchAnalysis = AnalyzeSwitchType(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(switchAnalysis)) comparisons.Add(switchAnalysis);
                    }

                    // Layout Analysis
                    if (productSpecs.ContainsKey("layout"))
                    {
                        var layoutAnalysis = AnalyzeKeyboardLayout(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(layoutAnalysis)) comparisons.Add(layoutAnalysis);
                    }

                    // Connectivity Analysis
                    if (productSpecs.ContainsKey("kết_nối"))
                    {
                        var connectivityAnalysis = AnalyzeKeyboardConnectivity(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(connectivityAnalysis)) comparisons.Add(connectivityAnalysis);
                    }

                    // Battery Analysis
                    if (productSpecs.ContainsKey("pin"))
                    {
                        var batteryAnalysis = AnalyzeKeyboardBattery(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(batteryAnalysis)) comparisons.Add(batteryAnalysis);
                    }
                    break;

                case "monitor":
                    // Resolution Comparison
                    if (productSpecs.ContainsKey("độ_phân_giải"))
                    {
                        var resolutionComparison = CompareMonitorResolution(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(resolutionComparison)) comparisons.Add(resolutionComparison);
                    }

                    // Refresh Rate Comparison
                    if (productSpecs.ContainsKey("tần_số_quét"))
                    {
                        var refreshComparison = CompareMonitorRefreshRate(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(refreshComparison)) comparisons.Add(refreshComparison);
                    }

                    // Panel Type Comparison
                    if (productSpecs.ContainsKey("tấm_nền"))
                    {
                        var panelComparison = CompareMonitorPanelType(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(panelComparison)) comparisons.Add(panelComparison);
                    }

                    // Size Comparison
                    if (productSpecs.ContainsKey("kích_thước"))
                    {
                        var sizeComparison = CompareMonitorSize(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(sizeComparison)) comparisons.Add(sizeComparison);
                    }

                    // Connectivity Comparison
                    if (productSpecs.ContainsKey("cổng_kết_nối"))
                    {
                        var connectivityComparison = CompareMonitorConnectivity(recommendedProduct, allProducts, allSpecs);
                        if (!string.IsNullOrEmpty(connectivityComparison)) comparisons.Add(connectivityComparison);
                    }
                    break;
            }

            return comparisons.Take(4).ToList();
        }

        private string CompareCPUSpecs(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var productCPU = allSpecs[recommendedProduct.ProductID]["cpu"].ToString();
            var competitorCPUs = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("cpu") 
                    ? allSpecs[p.ProductID]["cpu"].ToString() : "").Where(cpu => !string.IsNullOrEmpty(cpu)).ToList();

            // Intel Core hierarchy analysis
            var cpuScore = GetCPUPerformanceScore(productCPU);
            var avgCompetitorScore = competitorCPUs.Any() ? competitorCPUs.Average(GetCPUPerformanceScore) : 0;

            if (cpuScore > avgCompetitorScore + 2)
            {
                return $"💪 CPU vượt trội: {productCPU} mạnh hơn đáng kể so với đối thủ - hiệu năng xử lý nhanh hơn {((cpuScore - avgCompetitorScore) / avgCompetitorScore * 100):F0}%";
            }
            else if (cpuScore > avgCompetitorScore)
            {
                return $"⚡ CPU mạnh: {productCPU} cho hiệu năng tốt hơn {((cpuScore - avgCompetitorScore) / avgCompetitorScore * 100):F0}% so với trung bình";
            }

            return string.Empty;
        }

        private string CompareRAMSpecs(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var productRAM = ExtractNumericValue(allSpecs[recommendedProduct.ProductID]["ram"].ToString());
            var competitorRAMs = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("ram") 
                    ? ExtractNumericValue(allSpecs[p.ProductID]["ram"].ToString()) : 0).Where(ram => ram > 0).ToList();

            var maxCompetitorRAM = competitorRAMs.Any() ? competitorRAMs.Max() : 0;
            var avgCompetitorRAM = competitorRAMs.Any() ? competitorRAMs.Average() : 0;

            if (productRAM > maxCompetitorRAM)
            {
                return $"🚀 RAM dẫn đầu: {productRAM}GB - cao nhất trong danh sách, đảm bảo multitasking mượt mà với {(productRAM / 8):F0} ứng dụng nặng cùng lúc";
            }
            else if (productRAM > avgCompetitorRAM * 1.3)
            {
                return $"💾 RAM lớn: {productRAM}GB - nhiều hơn {(productRAM - avgCompetitorRAM):F0}GB so với trung bình, hỗ trợ tốt cho gaming và creative work";
            }

            return string.Empty;
        }

        private string CompareStorageSpecs(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var productStorage = allSpecs[recommendedProduct.ProductID]["storage"].ToString();
            var storageSize = ExtractNumericValue(productStorage);
            var isSSD = productStorage.ToLower().Contains("ssd");

            var competitorStorages = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("storage") 
                    ? allSpecs[p.ProductID]["storage"].ToString() : "").Where(s => !string.IsNullOrEmpty(s)).ToList();

            var competitorSSDCount = competitorStorages.Count(s => s.ToLower().Contains("ssd"));
            var avgStorageSize = competitorStorages.Any() ? competitorStorages.Average(s => ExtractNumericValue(s)) : 0;

            if (isSSD && competitorSSDCount < competitorStorages.Count)
            {
                return $"⚡ Ổ cứng SSD {storageSize}GB - nhanh gấp 10 lần HDD, khởi động trong 10 giây, load game/app tức thì";
            }
            else if (storageSize > avgStorageSize * 1.5)
            {
                return $"💽 Dung lượng lớn: {storageSize}GB - lưu trữ được {(storageSize / 100):F0} game AAA hoặc {(storageSize / 10):F0}K ảnh 4K";
            }

            return string.Empty;
        }

        private string CompareDPISpecs(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var productDPI = ExtractNumericValue(allSpecs[recommendedProduct.ProductID]["dpi"].ToString());
            var competitorDPIs = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("dpi") 
                    ? ExtractNumericValue(allSpecs[p.ProductID]["dpi"].ToString()) : 0).Where(dpi => dpi > 0).ToList();

            var maxCompetitorDPI = competitorDPIs.Any() ? competitorDPIs.Max() : 0;

            if (productDPI >= 12000 && productDPI > maxCompetitorDPI)
            {
                return $"🎯 DPI cao nhất: {productDPI} DPI - chính xác pixel-perfect cho FPS gaming và design, cao hơn {productDPI - maxCompetitorDPI} DPI so với đối thủ";
            }
            else if (productDPI >= 8000)
            {
                return $"🎮 DPI gaming: {productDPI} DPI - tối ưu cho gaming competitive, tracking chính xác trên mọi surface";
            }

            return string.Empty;
        }

        private string ComparePollingRateSpecs(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var productPolling = ExtractNumericValue(allSpecs[recommendedProduct.ProductID]["polling_rate"].ToString());
            var competitorPollings = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("polling_rate") 
                    ? ExtractNumericValue(allSpecs[p.ProductID]["polling_rate"].ToString()) : 0).Where(p => p > 0).ToList();

            var maxCompetitorPolling = competitorPollings.Any() ? competitorPollings.Max() : 0;

            if (productPolling >= 1000 && productPolling > maxCompetitorPolling)
            {
                return $"⚡ Tần số quét 1000Hz - phản hồi 1ms, nhanh gấp {(productPolling / (maxCompetitorPolling > 0 ? maxCompetitorPolling : 125)):F0} lần, zero input lag cho esports";
            }
            else if (productPolling >= 500)
            {
                return $"🏃 Tần số quét {productPolling}Hz - phản hồi {(1000.0 / productPolling):F1}ms, rất tốt cho gaming competitive";
            }

            return string.Empty;
        }

        private string AnalyzeSwitchType(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var switchType = allSpecs[recommendedProduct.ProductID]["switch"].ToString().ToLower();

            if (switchType.Contains("mechanical") || switchType.Contains("cherry mx") || switchType.Contains("blue") || switchType.Contains("red"))
            {
                var switchDetails = GetSwitchTypeDetails(switchType);
                return $"⌨️ Switch cơ {switchType}: {switchDetails} - bền 50 triệu lần bấm, tactile feedback tốt";
            }
            else if (switchType.Contains("optical") || switchType.Contains("laser"))
            {
                return $"🔥 Switch quang học - phản hồi 0.2ms, bền gấp 3 lần switch cơ thường, zero debounce";
            }

            return string.Empty;
        }

        // Enhanced Keyboard Analysis Methods
        private string AnalyzeKeyboardLayout(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var layout = allSpecs[recommendedProduct.ProductID]["layout"].ToString().ToLower();

            if (layout.Contains("65%") || layout.Contains("compact"))
            {
                return $"⌨️ Layout 65% compact - tiết kiệm không gian bàn làm việc, phù hợp cho gaming và travel";
            }
            else if (layout.Contains("tkl") || layout.Contains("tenkeyless"))
            {
                return $"🎮 Layout TKL - cân bằng hoàn hảo giữa chức năng và không gian, tối ưu cho gaming";
            }
            else if (layout.Contains("full") || layout.Contains("104"))
            {
                return $"💼 Layout Full-size - đầy đủ chức năng cho văn phòng, có numpad tiện lợi cho data entry";
            }

            return string.Empty;
        }

        private string AnalyzeKeyboardConnectivity(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var connectivity = allSpecs[recommendedProduct.ProductID]["kết_nối"].ToString().ToLower();

            if (connectivity.Contains("triple mode") || (connectivity.Contains("wireless") && connectivity.Contains("usb")))
            {
                return $"🔗 Triple Mode: USB-C wired + 2.4GHz wireless + Bluetooth - linh hoạt tối đa cho mọi thiết bị";
            }
            else if (connectivity.Contains("wireless") || connectivity.Contains("bluetooth"))
            {
                return $"📶 Kết nối không dây - tự do di chuyển, pin lâu, tương thích đa thiết bị";
            }
            else if (connectivity.Contains("usb-c"))
            {
                return $"⚡ USB-C wired - charging nhanh, tương thích laptop hiện đại, zero latency";
            }

            return string.Empty;
        }

        private string AnalyzeKeyboardBattery(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var batteryInfo = allSpecs[recommendedProduct.ProductID]["pin"].ToString();
            var batteryCapacity = ExtractNumericValue(batteryInfo);

            if (batteryCapacity >= 3000)
            {
                return $"🔋 Pin khủng {batteryCapacity}mAh - sử dụng liên tục 6+ tháng, sạc 1 lần/năm";
            }
            else if (batteryCapacity >= 2000)
            {
                return $"🔋 Pin lớn {batteryCapacity}mAh - sử dụng 3-4 tháng, tiết kiệm điện tối ưu";
            }
            else if (batteryCapacity >= 1000)
            {
                return $"🔋 Pin {batteryCapacity}mAh - đủ dùng 1-2 tháng cho work from home";
            }

            return string.Empty;
        }

        // Monitor Analysis Methods
        private string CompareMonitorResolution(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var productSpecs = allSpecs[recommendedProduct.ProductID];
            var resolution = productSpecs.ContainsKey("độ_phân_giải") ? 
                productSpecs["độ_phân_giải"].ToString() : "";

            var competitors4K = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID &&
                allSpecs.ContainsKey(p.ProductID)).Count(p => {
                    var compSpecs = allSpecs[p.ProductID];
                    var compRes = compSpecs.ContainsKey("độ_phân_giải") ? 
                        compSpecs["độ_phân_giải"].ToString() : "";
                    return compRes.ToLower().Contains("4k") || compRes.Contains("3840") || compRes.Contains("2560");
                });

            if (resolution.ToLower().Contains("4k") || resolution.Contains("3840"))
            {
                return $"🎯 Độ phân giải 4K UHD (3840x2160) - chi tiết gấp 4 lần Full HD, hoàn hảo cho design và content creation";
            }
            else if (resolution.ToLower().Contains("2k") || resolution.Contains("2560"))
            {
                return $"📺 Độ phân giải 2K QHD (2560x1440) - sweet spot cho gaming và productivity, cân bằng hiệu năng và chất lượng";
            }
            else if (resolution.ToLower().Contains("fhd") || resolution.Contains("1920"))
            {
                if (competitors4K == 0)
                {
                    return $"💻 Full HD 1920x1080 - tiêu chuẩn gaming, hiệu năng cao với card đồ họa entry-level";
                }
            }

            return string.Empty;
        }

        private string CompareMonitorRefreshRate(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var productRefreshRate = ExtractNumericValue(allSpecs[recommendedProduct.ProductID]["tần_số_quét"].ToString());
            var competitorRefreshRates = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("tần_số_quét") 
                    ? ExtractNumericValue(allSpecs[p.ProductID]["tần_số_quét"].ToString()) : 60)
                .Where(r => r > 0).ToList();

            var maxCompetitorRefresh = competitorRefreshRates.Any() ? competitorRefreshRates.Max() : 60;

            if (productRefreshRate >= 240 && productRefreshRate > maxCompetitorRefresh)
            {
                return $"🚀 Tần số quét 240Hz - esports level, motion blur = 0, competitive advantage cực đại cho FPS games";
            }
            else if (productRefreshRate >= 144 && productRefreshRate > maxCompetitorRefresh)
            {
                return $"⚡ Tần số quét 144Hz - gaming mượt mà hoàn hảo, responsive gấp 2.4 lần màn hình 60Hz thường";
            }
            else if (productRefreshRate >= 120)
            {
                return $"🎮 Tần số quét 120Hz - gaming experience tuyệt vời, đáng kể nâng cấp từ 60Hz";
            }

            return string.Empty;
        }

        private string CompareMonitorPanelType(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var panelType = allSpecs[recommendedProduct.ProductID]["tấm_nền"].ToString().ToUpper();

            if (panelType.Contains("IPS"))
            {
                return $"🌈 Panel IPS - màu sắc chính xác 99% sRGB, góc nhìn 178°, hoàn hảo cho design và photo editing";
            }
            else if (panelType.Contains("VA"))
            {
                return $"🌙 Panel VA - contrast ratio cao 3000:1, màu đen sâu tuyệt đối, tối ưu cho xem phim và dark scenes";
            }
            else if (panelType.Contains("TN"))
            {
                return $"⚡ Panel TN - response time 1ms cực nhanh, input lag thấp nhất, competitive gaming advantage";
            }
            else if (panelType.Contains("OLED"))
            {
                return $"✨ Panel OLED - màu đen tuyệt đối, contrast vô hạn, HDR cực đỉnh cho cinematic experience";
            }

            return string.Empty;
        }

        private string CompareMonitorSize(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var screenSize = ExtractNumericValue(allSpecs[recommendedProduct.ProductID]["kích_thước"].ToString());
            var competitorSizes = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("kích_thước") 
                    ? ExtractNumericValue(allSpecs[p.ProductID]["kích_thước"].ToString()) : 0)
                .Where(s => s > 0).ToList();

            var maxCompetitorSize = competitorSizes.Any() ? competitorSizes.Max() : 0;

            if (screenSize >= 32 && screenSize > maxCompetitorSize)
            {
                return $"📺 Màn hình khủng {screenSize}\" - tầm nhìn panoramic, multitasking 4+ windows, immersive gaming experience";
            }
            else if (screenSize >= 27 && screenSize > maxCompetitorSize)
            {
                return $"🖥️ Màn hình lớn {screenSize}\" - sweet spot cho productivity và gaming, vừa đủ không gian bàn làm việc";
            }
            else if (screenSize >= 24)
            {
                return $"💻 Màn hình {screenSize}\" - kích thước tiêu chuẩn, phù hợp mọi setup từ compact đến full battlestation";
            }

            return string.Empty;
        }

        private string CompareMonitorConnectivity(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var connectivity = allSpecs[recommendedProduct.ProductID]["cổng_kết_nối"].ToString().ToLower();

            if (connectivity.Contains("usb-c") && connectivity.Contains("displayport") && connectivity.Contains("hdmi"))
            {
                return $"🔗 Kết nối đa dạng: USB-C + DisplayPort + HDMI - tương thích laptop, PC, console, 1 cable charge + display";
            }
            else if (connectivity.Contains("usb-c"))
            {
                return $"⚡ USB-C - charge laptop + hiển thị 1 cable, tương thích MacBook và laptop hiện đại";
            }
            else if (connectivity.Contains("displayport") && connectivity.Contains("hdmi"))
            {
                return $"🎮 DisplayPort + HDMI - dual input tiện lợi, kết nối PC gaming + console cùng lúc";
            }

            return string.Empty;
        }

        // Enhanced Mouse Analysis Methods
        private string AnalyzeMouseSensor(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var sensor = allSpecs[recommendedProduct.ProductID]["cảm_biến"].ToString().ToUpper();

            if (sensor.Contains("HERO") || sensor.Contains("FOCUS"))
            {
                return $"🎯 Cảm biến {sensor} - flagship sensor, zero acceleration, tracking chính xác trên mọi surface";
            }
            else if (sensor.Contains("PMW3360") || sensor.Contains("3360"))
            {
                return $"⚡ Sensor PMW3360 - gaming sensor huyền thoại, zero smoothing, perfect tracking";
            }
            else if (sensor.Contains("OPTICAL") || sensor.Contains("LASER"))
            {
                return $"📡 Sensor quang học - độ chính xác cao, ít lỗi tracking trên cloth pad";
            }

            return string.Empty;
        }

        private string AnalyzeMouseWeight(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var weight = ExtractNumericValue(allSpecs[recommendedProduct.ProductID]["trọng_lượng"].ToString());
            var competitorWeights = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("trọng_lượng") 
                    ? ExtractNumericValue(allSpecs[p.ProductID]["trọng_lượng"].ToString()) : 0)
                .Where(w => w > 0).ToList();

            var avgCompetitorWeight = competitorWeights.Any() ? competitorWeights.Average() : 100;

            if (weight <= 70 && weight < avgCompetitorWeight)
            {
                return $"🪶 Siêu nhẹ {weight}g - flick shot nhanh như chớp, giảm fatigue trong gaming marathon";
            }
            else if (weight <= 90 && weight < avgCompetitorWeight)
            {
                return $"⚡ Nhẹ {weight}g - balance hoàn hảo cho precision và speed, comfortable cho long session";
            }
            else if (weight >= 120)
            {
                return $"🏋️ Trọng lượng {weight}g - stability cao cho precision, phù hợp palm grip và control style";
            }

            return string.Empty;
        }

        private string AnalyzeMouseConnectivity(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var connectivity = allSpecs[recommendedProduct.ProductID]["kết_nối"].ToString().ToLower();

            if (connectivity.Contains("lightspeed") || connectivity.Contains("2.4ghz"))
            {
                return $"📶 LIGHTSPEED 2.4GHz - latency 1ms như wired, battery 140+ giờ, pro gaming wireless";
            }
            else if (connectivity.Contains("wireless") && connectivity.Contains("wired"))
            {
                return $"🔗 Dual mode: Wireless + Wired - linh hoạt cho gaming và travel, zero downtime";
            }
            else if (connectivity.Contains("wireless"))
            {
                return $"📡 Kết nối không dây - tự do movement, clean setup, battery life tối ưu";
            }
            else if (connectivity.Contains("usb"))
            {
                return $"⚡ USB wired - zero latency, không lo battery, reliable cho competitive gaming";
            }

            return string.Empty;
        }

        private string AnalyzeMouseBattery(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var batteryInfo = allSpecs[recommendedProduct.ProductID]["thời_lượng_pin"].ToString();
            var batteryHours = ExtractNumericValue(batteryInfo);

            if (batteryHours >= 100)
            {
                return $"🔋 Pin siêu lâu {batteryHours}h - gaming 1+ tháng không sạc, PowerPlay compatible";
            }
            else if (batteryHours >= 50)
            {
                return $"🔋 Pin lâu {batteryHours}h - gaming 2+ tuần, quick charge 15 phút = 8h sử dụng";
            }
            else if (batteryHours >= 20)
            {
                return $"🔋 Pin {batteryHours}h - đủ cho gaming session dài, USB-C charging nhanh";
            }

            return string.Empty;
        }

        private string GetSwitchTypeDetails(string switchType)
        {
            if (switchType.Contains("blue")) return "click rõ ràng, phù hợp typing";
            if (switchType.Contains("red")) return "linear, tối ưu gaming";
            if (switchType.Contains("brown")) return "tactile nhẹ, đa năng";
            return "chất lượng cao";
        }

        private double GetCPUPerformanceScore(string cpu)
        {
            var cpuLower = cpu.ToLower();
            
            // Intel Core series scoring
            if (cpuLower.Contains("i9")) return 10;
            if (cpuLower.Contains("i7")) return 8.5;
            if (cpuLower.Contains("i5")) return 7;
            if (cpuLower.Contains("i3")) return 5;
            
            // AMD Ryzen series
            if (cpuLower.Contains("ryzen 9")) return 9.5;
            if (cpuLower.Contains("ryzen 7")) return 8;
            if (cpuLower.Contains("ryzen 5")) return 6.5;
            if (cpuLower.Contains("ryzen 3")) return 4.5;
            
            return 3; // Default for older/unknown CPUs
        }

        private string CompareRefreshRateSpecs(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var productRefreshRate = allSpecs[recommendedProduct.ProductID].ContainsKey("refresh_rate_hz") 
                ? ExtractNumericValue(allSpecs[recommendedProduct.ProductID]["refresh_rate_hz"].ToString()) : 0;

            var competitorRefreshRates = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("refresh_rate_hz") 
                    ? ExtractNumericValue(allSpecs[p.ProductID]["refresh_rate_hz"].ToString()) : 0)
                .Where(rate => rate > 0).ToList();

            var maxCompetitorRefresh = competitorRefreshRates.Any() ? competitorRefreshRates.Max() : 60;

            if (productRefreshRate >= 165 && productRefreshRate > maxCompetitorRefresh)
            {
                return $"🚀 Màn hình {productRefreshRate}Hz dẫn đầu - gaming siêu mượt, competitive advantage rõ rệt so với {maxCompetitorRefresh}Hz của đối thủ";
            }
            else if (productRefreshRate >= 144)
            {
                return $"⚡ Màn hình {productRefreshRate}Hz gaming - 2.4x mượt hơn 60Hz thường, ideal cho FPS games và racing";
            }
            else if (productRefreshRate >= 120)
            {
                return $"✨ Màn hình {productRefreshRate}Hz smooth - tăng 100% độ mượt so với 60Hz, tốt cho gaming casual và productivity";
            }

            return string.Empty;
        }

        private string CompareDisplayQuality(Product recommendedProduct, List<Product> allProducts, Dictionary<int, Dictionary<string, object>> allSpecs)
        {
            var productSpecs = allSpecs[recommendedProduct.ProductID];
            var resolution = productSpecs.ContainsKey("resolution_class") ? productSpecs["resolution_class"].ToString() : "";
            var panelType = productSpecs.ContainsKey("panel_type") ? productSpecs["panel_type"].ToString() : "";

            var advantages = new List<string>();

            // Resolution advantage
            if (resolution == "4K")
            {
                var competitors4K = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID &&
                    allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("resolution_class") &&
                    allSpecs[p.ProductID]["resolution_class"].ToString() == "4K").Count();
                
                if (competitors4K == 0)
                {
                    advantages.Add("4K Ultra HD - 4x chi tiết hơn FHD, perfect cho content creation và gaming 4K");
                }
            }
            else if (resolution == "2K" || resolution == "QHD")
            {
                advantages.Add("2K QHD - 1.8x chi tiết hơn FHD, sweet spot cho gaming performance và chất lượng");
            }

            // Panel type advantage
            if (panelType == "IPS")
            {
                var competitorIPS = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID &&
                    allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("panel_type") &&
                    allSpecs[p.ProductID]["panel_type"].ToString() == "IPS").Count();

                if (competitorIPS == 0)
                {
                    advantages.Add("Panel IPS cao cấp - màu sắc chính xác 100% sRGB, góc nhìn 178°, superior cho design work");
                }
            }
            else if (panelType == "OLED" || panelType == "AMOLED")
            {
                advantages.Add("Panel OLED premium - contrast ratio vô cực, black tuyệt đối, HDR stunning");
            }

            return advantages.Any() ? $"🖥️ {string.Join(" + ", advantages)}" : string.Empty;
        }

        private List<string> GeneratePerformanceAnalysis(Product recommendedProduct, List<Product> allProducts, 
            Dictionary<string, object> productSpecs, string comparisonType)
        {
            var analysis = new List<string>();

            switch (comparisonType.ToLower())
            {
                case "laptop":
                    if (productSpecs.ContainsKey("cpu") && productSpecs.ContainsKey("ram"))
                    {
                        var cpu = productSpecs["cpu"].ToString();
                        var ram = ExtractNumericValue(productSpecs["ram"].ToString());
                        
                        if (cpu.ToLower().Contains("i7") && ram >= 16)
                        {
                            analysis.Add("🚀 Cấu hình mạnh mẽ: có thể chạy Adobe Premiere Pro, Photoshop đồng thời với 20+ tab Chrome mà không lag");
                        }
                        else if (cpu.ToLower().Contains("i5") && ram >= 8)
                        {
                            analysis.Add("💻 Hiệu năng ổn định: đa nhiệm tốt với Office, browsing, và streaming video 4K");
                        }
                    }
                    break;

                case "mouse":
                    if (productSpecs.ContainsKey("dpi") && productSpecs.ContainsKey("polling_rate"))
                    {
                        var dpi = ExtractNumericValue(productSpecs["dpi"].ToString());
                        var polling = ExtractNumericValue(productSpecs["polling_rate"].ToString());
                        
                        if (dpi >= 12000 && polling >= 1000)
                        {
                            analysis.Add("🎯 Gaming performance elite: đủ độ nhạy cho 360° flick shots trong CS2/Valorant, zero pixel skipping");
                        }
                        else if (dpi >= 6000 && polling >= 500)
                        {
                            analysis.Add("🎮 Gaming performance tốt: tracking mượt mà cho FPS games, MMO, và precision work");
                        }
                    }
                    break;

                case "keyboard":
                    if (productSpecs.ContainsKey("switch_type"))
                    {
                        var switchType = productSpecs["switch_type"].ToString().ToLower();
                        if (switchType.Contains("mechanical"))
                        {
                            analysis.Add("⌨️ Cơ học cao cấp: tactile feedback rõ ràng, gõ 8 giờ/ngày không mỏi, bền 50 triệu lần bấm");
                        }
                    }
                    break;
            }

            return analysis.Take(1).ToList();
        }

        private List<string> GenerateCompetitivePositioning(Product recommendedProduct, List<Product> allProducts, 
            Dictionary<int, Dictionary<string, object>> allSpecs, string comparisonType)
        {
            var positioning = new List<string>();
            var productSpecs = allSpecs[recommendedProduct.ProductID];
            var productPrice = recommendedProduct.GetEffectivePrice();

            // Find the most expensive competitor
            var mostExpensiveCompetitor = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .OrderByDescending(p => p.GetEffectivePrice()).FirstOrDefault();

            if (mostExpensiveCompetitor != null)
            {
                var priceDiff = mostExpensiveCompetitor.GetEffectivePrice() - productPrice;
                if (priceDiff > 2000000) // > 2 triệu VND difference
                {
                    positioning.Add($"💰 Tiết kiệm {priceDiff / 1000000:F1} triệu so với {mostExpensiveCompetitor.Name} nhưng hiệu năng tương đương");
                }
            }

            // Find spec advantages
            if (comparisonType.ToLower() == "laptop" && productSpecs.ContainsKey("ram"))
            {
                var productRAM = ExtractNumericValue(productSpecs["ram"].ToString());
                var competitorRAMs = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                    .Select(p => allSpecs.ContainsKey(p.ProductID) && allSpecs[p.ProductID].ContainsKey("ram") 
                        ? ExtractNumericValue(allSpecs[p.ProductID]["ram"].ToString()) : 0)
                    .Where(ram => ram > 0).ToList();

                if (competitorRAMs.Any() && productRAM > competitorRAMs.Max())
                {
                    positioning.Add($"🔥 RAM cao nhất ({productRAM}GB) - nhiều hơn {productRAM - competitorRAMs.Max()}GB so với đối thủ gần nhất");
                }
            }

            return positioning.Take(1).ToList();
        }

        private List<string> GenerateDetailedValueAnalysis(Product recommendedProduct, List<Product> allProducts, 
            Dictionary<string, object> productSpecs)
        {
            var analysis = new List<string>();
            var price = recommendedProduct.GetEffectivePrice();
            var avgPrice = allProducts.Average(p => p.GetEffectivePrice());

            // Performance per dollar analysis
            var performanceScore = CalculateOverallPerformanceScore(productSpecs);
            var pricePerformanceRatio = performanceScore / ((double)price / 1000000); // Score per million VND

            var competitorRatios = allProducts.Where(p => p.ProductID != recommendedProduct.ProductID)
                .Select(p => {
                    var specs = GetProductSpecifications(p);
                    var perfScore = CalculateOverallPerformanceScore(specs);
                    return perfScore / ((double)p.GetEffectivePrice() / 1000000);
                }).ToList();

            if (competitorRatios.Any() && pricePerformanceRatio > competitorRatios.Max())
            {
                analysis.Add($"⭐ Giá trị tốt nhất: {pricePerformanceRatio:F1} điểm hiệu năng/triệu VND - cao hơn {(pricePerformanceRatio - competitorRatios.Max()):F1} điểm so với đối thủ");
            }

            return analysis.Take(1).ToList();
        }

        private double CalculateOverallPerformanceScore(Dictionary<string, object> specs)
        {
            double score = 5; // Base score

            if (specs.ContainsKey("cpu"))
            {
                score += GetCPUPerformanceScore(specs["cpu"].ToString()) * 0.3;
            }

            if (specs.ContainsKey("ram"))
            {
                var ram = ExtractNumericValue(specs["ram"].ToString());
                score += Math.Min(ram / 4, 10) * 0.2; // Max 10 points for 40GB+ RAM
            }

            if (specs.ContainsKey("dpi"))
            {
                var dpi = ExtractNumericValue(specs["dpi"].ToString());
                score += Math.Min(dpi / 1000, 10) * 0.3; // Max 10 points for 10K+ DPI
            }

            return score;
        }

        private List<string> GenerateTechnicalAdvantages(Product recommendedProduct, List<Product> allProducts, 
            Dictionary<string, object> productSpecs, string comparisonType)
        {
            var advantages = new List<string>();

            // Check for premium features
            if (productSpecs.ContainsKey("storage") && productSpecs["storage"].ToString().ToLower().Contains("nvme"))
            {
                advantages.Add("⚡ NVMe SSD - tốc độ đọc 3,500MB/s, nhanh gấp 7 lần SATA SSD thường");
            }

            if (comparisonType.ToLower() == "mouse" && productSpecs.ContainsKey("sensor"))
            {
                var sensor = productSpecs["sensor"].ToString().ToLower();
                if (sensor.Contains("optical") || sensor.Contains("laser"))
                {
                    advantages.Add("🔬 Sensor quang học chính xác - zero acceleration, perfect tracking trên mọi surface");
                }
            }

            return advantages.Take(1).ToList();
        }

        private List<string> GenerateUserExperienceInsights(Product recommendedProduct, Dictionary<string, object> productSpecs, string comparisonType)
        {
            var insights = new List<string>();

            switch (comparisonType.ToLower())
            {
                case "laptop":
                    if (productSpecs.ContainsKey("screen_size"))
                    {
                        var screenSize = ExtractNumericValue(productSpecs["screen_size"].ToString());
                        if (screenSize >= 15)
                        {
                            insights.Add($"👀 Màn hình lớn {screenSize}\" - thoải mái làm việc với 2 app side-by-side, xem phim sắc nét");
                        }
                    }
                    break;

                case "mouse":
                    if (productSpecs.ContainsKey("weight"))
                    {
                        var weight = ExtractNumericValue(productSpecs["weight"].ToString());
                        if (weight <= 80)
                        {
                            insights.Add($"🪶 Nhẹ chỉ {weight}g - không mỏi tay sau 8 giờ gaming marathon, flick shots dễ dàng");
                        }
                    }
                    break;

                case "keyboard":
                    insights.Add("✍️ Trải nghiệm gõ premium - typing accuracy tăng 15%, WPM speed cải thiện rõ rệt");
                    break;
            }

            return insights.Take(1).ToList();
        }

        private List<string> GenerateBrandEcosystemAdvantages(Product recommendedProduct, List<Product> allProducts)
        {
            var advantages = new List<string>();
            var brand = recommendedProduct.Brand.ToLower();

            var ecosystemBenefits = new Dictionary<string, string>
            {
                ["apple"] = "🍎 Ecosystem hoàn hảo với iPhone/iPad - AirDrop, Universal Control, Handoff seamless",
                ["dell"] = "🏢 ProSupport Plus - hỗ trợ 24/7, onsite service, accidental damage protection",
                ["hp"] = "🛡️ HP Wolf Security built-in - bảo mật hardware-level, safe browsing tự động",
                ["lenovo"] = "💼 ThinkShield security - fingerprint, IR camera, BIOS protection enterprise-grade",
                ["asus"] = "🎮 ROG ecosystem - Armoury Crate sync, Aura lighting effects, exclusive gaming features",
                ["razer"] = "🌈 Razer Synapse - cloud sync settings, macro programming, Chroma RGB ecosystem",
                ["logitech"] = "🔧 Logitech G HUB - DPI profiles, button mapping, RGB sync với headset/keyboard"
            };

            if (ecosystemBenefits.ContainsKey(brand))
            {
                advantages.Add(ecosystemBenefits[brand]);
            }

            return advantages.Take(1).ToList();
        }

        private List<string> GeneratePurchaseDecisionFactors(Product recommendedProduct, List<Product> allProducts)
        {
            var factors = new List<string>();

            // Warranty and support
            factors.Add($"🛡️ Bảo hành chính hãng {recommendedProduct.Brand} - service center toàn quốc, đổi mới trong 30 ngày");

            // Stock urgency if limited
            if (recommendedProduct.Stock <= 10 && recommendedProduct.Stock > 0)
            {
                factors.Add($"⚠️ Chỉ còn {recommendedProduct.Stock} chiếc - popularity cao, nên đặt hàng sớm");
            }

            return factors.Take(1).ToList();
        }

        private string GetCategorySpecificDetails(Product product, string category, Dictionary<string, object> specs, string comparisonType)
        {
            switch (category.ToLower())
            {
                case "hiệu năng":
                    return comparisonType.ToLower() switch
                    {
                        "laptop" => GetLaptopPerformanceDetails(specs),
                        "mouse" => GetMousePerformanceDetails(specs),
                        "keyboard" => GetKeyboardPerformanceDetails(specs),
                        "headphone" => GetHeadphoneAudioDetails(specs),
                        _ => "cấu hình mạnh mẽ"
                    };
                case "thiết kế":
                    return "thiết kế hiện đại, chất liệu cao cấp";
                case "tính năng":
                    return "tính năng đa dạng, hỗ trợ tối ưu";
                default:
                    return "chất lượng vượt trội";
            }
        }

        private string GetLaptopPerformanceDetails(Dictionary<string, object> specs)
        {
            var details = new List<string>();
            if (specs.ContainsKey("cpu")) details.Add($"CPU {specs["cpu"]}");
            if (specs.ContainsKey("ram")) details.Add($"RAM {specs["ram"]}");
            if (specs.ContainsKey("gpu") && !specs["gpu"].ToString().Contains("không")) details.Add($"GPU {specs["gpu"]}");
            return details.Any() ? string.Join(", ", details) : "cấu hình mạnh mẽ";
        }

        private string GetMousePerformanceDetails(Dictionary<string, object> specs)
        {
            var details = new List<string>();
            if (specs.ContainsKey("dpi")) details.Add($"{specs["dpi"]} DPI");
            if (specs.ContainsKey("polling_rate")) details.Add($"{specs["polling_rate"]} Hz");
            return details.Any() ? string.Join(", ", details) : "độ chính xác cao";
        }

        private string GetKeyboardPerformanceDetails(Dictionary<string, object> specs)
        {
            var details = new List<string>();
            if (specs.ContainsKey("switch_type")) details.Add($"Switch {specs["switch_type"]}");
            if (specs.ContainsKey("response_time")) details.Add($"phản hồi {specs["response_time"]}ms");
            return details.Any() ? string.Join(", ", details) : "switch chất lượng cao";
        }

        private string GetHeadphoneAudioDetails(Dictionary<string, object> specs)
        {
            var details = new List<string>();
            if (specs.ContainsKey("frequency_response")) details.Add($"tần số {specs["frequency_response"]}");
            if (specs.ContainsKey("driver_size")) details.Add($"driver {specs["driver_size"]}");
            return details.Any() ? string.Join(", ", details) : "âm thanh chất lượng cao";
        }

        private List<string> AnalyzeCompetitiveAdvantages(Product product, List<Product> allProducts, 
            Dictionary<int, Dictionary<string, decimal>> detailedScores, string comparisonType)
        {
            var advantages = new List<string>();
            var productScores = detailedScores[product.ProductID];

            // Find categories where this product dominates
            foreach (var category in productScores.Keys)
            {
                var isLeading = true;
                foreach (var otherProduct in allProducts.Where(p => p.ProductID != product.ProductID))
                {
                    if (detailedScores.ContainsKey(otherProduct.ProductID) && 
                        detailedScores[otherProduct.ProductID].ContainsKey(category))
                    {
                        if (detailedScores[otherProduct.ProductID][category] >= productScores[category])
                        {
                            isLeading = false;
                            break;
                        }
                    }
                }

                if (isLeading && productScores[category] >= 7.0m)
                {
                    advantages.Add($"🎯 Dẫn đầu về {category.ToLower()} so với tất cả đối thủ");
                }
            }

            return advantages.Take(2).ToList();
        }

        private string AnalyzePriceValue(Product product, List<Product> allProducts, decimal overallScore)
        {
            var price = product.GetEffectivePrice();
            var avgPrice = allProducts.Average(p => p.GetEffectivePrice());
            var maxPrice = allProducts.Max(p => p.GetEffectivePrice());
            var minPrice = allProducts.Min(p => p.GetEffectivePrice());

            if (price == minPrice && overallScore >= 7.5m)
            {
                return $"💰 Giá tốt nhất ({price / 1000000:F1}tr) với chất lượng hàng đầu - giá trị vượt trội";
            }
            else if (price < avgPrice * 0.8m)
            {
                return $"💰 Tiết kiệm {((avgPrice - price) / 1000000):F1} triệu so với trung bình nhưng vẫn chất lượng cao";
            }
            else if (price > avgPrice * 1.3m && overallScore >= 8.5m)
            {
                return $"💎 Cao cấp nhất với chất lượng đáng giá mức đầu tư {price / 1000000:F1} triệu";
            }

            return string.Empty;
        }

        private List<string> GetPracticalBenefits(Product product, string comparisonType, Dictionary<string, decimal> scores)
        {
            var benefits = new List<string>();

            switch (comparisonType.ToLower())
            {
                case "laptop":
                    if (scores.ContainsKey("Hiệu năng") && scores["Hiệu năng"] >= 8.0m)
                        benefits.Add("⚡ Xử lý mượt mà các tác vụ nặng, multitasking hiệu quả");
                    if (scores.ContainsKey("Thiết kế") && scores["Thiết kế"] >= 7.5m)
                        benefits.Add("📱 Di động tuyệt vời, phù hợp làm việc mọi nơi");
                    break;
                case "mouse":
                    if (scores.ContainsKey("Hiệu năng") && scores["Hiệu năng"] >= 8.0m)
                        benefits.Add("🎯 Tracking chính xác, click responsive, phù hợp cả gaming và office");
                    break;
                case "keyboard":
                    if (scores.ContainsKey("Hiệu năng") && scores["Hiệu năng"] >= 8.0m)
                        benefits.Add("⌨️ Gõ thoải mái cả ngày, giảm mỏi tay");
                    break;
            }

            return benefits;
        }

        private List<string> GetTechnicalSuperiority(Product product, List<Product> allProducts, string comparisonType, Dictionary<string, object> specs)
        {
            var superiority = new List<string>();

            // Check for latest technology
            if (comparisonType.ToLower() == "laptop")
            {
                if (specs.ContainsKey("cpu") && specs["cpu"].ToString().Contains("Intel Core i7"))
                    superiority.Add("🔥 CPU Intel Core i7 thế hệ mới - hiệu năng hàng đầu");
                if (specs.ContainsKey("storage") && specs["storage"].ToString().ToLower().Contains("ssd"))
                    superiority.Add("⚡ SSD tốc độ cao - khởi động và load ứng dụng siêu nhanh");
            }

            return superiority.Take(1).ToList();
        }

        private string AnalyzeBrandReliability(Product product, List<Product> allProducts)
        {
            var premiumBrands = new Dictionary<string, string>
            {
                ["Apple"] = "đẳng cấp quốc tế, bảo hành toàn cầu",
                ["Dell"] = "tin cậy doanh nghiệp, hỗ trợ kỹ thuật tốt",
                ["HP"] = "thương hiệu lâu đời, dịch vụ sau bán hàng tốt",
                ["Lenovo"] = "chất lượng bền bỉ, thiết kế đáng tin cậy",
                ["Asus"] = "gaming performance mạnh mẽ",
                ["MSI"] = "chuyên gaming, cooling system tốt",
                ["Razer"] = "gaming gear cao cấp, RGB chuyên nghiệp",
                ["Logitech"] = "ergonomic design, độ bền cao"
            };

            return premiumBrands.ContainsKey(product.Brand) 
                ? $"🏆 {product.Brand} - {premiumBrands[product.Brand]}"
                : string.Empty;
        }

        private void AddLaptopSpecificReasons(List<string> reasons, Product product, Dictionary<string, decimal> scores)
        {
            if (scores.ContainsKey("Hiệu năng") && scores["Hiệu năng"] >= 8.0m)
            {
                reasons.Add("⚡ Hiệu năng mạnh mẽ, phù hợp công việc nặng và gaming");
            }
            if (scores.ContainsKey("Thiết kế") && scores["Thiết kế"] >= 7.5m)
            {
                reasons.Add("📱 Thiết kế mỏng nhẹ, dễ dàng di chuyển");
            }
        }

        private void AddMouseSpecificReasons(List<string> reasons, Product product, Dictionary<string, decimal> scores)
        {
            if (scores.ContainsKey("Hiệu năng") && scores["Hiệu năng"] >= 8.0m)
            {
                reasons.Add("🎯 Độ chính xác cao, tốc độ phản hồi nhanh");
            }
            if (scores.ContainsKey("Thiết kế") && scores["Thiết kế"] >= 7.5m)
            {
                reasons.Add("👋 Thiết kế ergonomic, thoải mái sử dụng lâu");
            }
        }

        private void AddKeyboardSpecificReasons(List<string> reasons, Product product, Dictionary<string, decimal> scores)
        {
            if (scores.ContainsKey("Hiệu năng") && scores["Hiệu năng"] >= 8.0m)
            {
                reasons.Add("⌨️ Switch chất lượng cao, trải nghiệm gõ tuyệt vời");
            }
            if (scores.ContainsKey("Tính năng") && scores["Tính năng"] >= 7.5m)
            {
                reasons.Add("🌈 Tính năng đa dạng, hỗ trợ tối ưu công việc");
            }
        }

        private void AddHeadphoneSpecificReasons(List<string> reasons, Product product, Dictionary<string, decimal> scores)
        {
            if (scores.ContainsKey("audio") && scores["audio"] >= 8.0m)
            {
                reasons.Add("🎵 Chất lượng âm thanh vượt trội, trải nghiệm immersive");
            }
            if (scores.ContainsKey("comfort") && scores["comfort"] >= 7.5m)
            {
                reasons.Add("😌 Thiết kế thoải mái, phù hợp sử dụng nhiều giờ");
            }
        }

        private List<string> GenerateAnalysisReasons(Product recommendedProduct, List<Product> allProducts, string comparisonType)
        {
            var reasons = new List<string>();

            // Price comparison
            var lowestPrice = allProducts.Min(p => p.GetEffectivePrice());
            if (recommendedProduct.GetEffectivePrice() == lowestPrice)
            {
                reasons.Add("💰 Có mức giá tốt nhất trong danh sách so sánh");
            }

            // Rating comparison
            var avgRating = recommendedProduct.Reviews?.Any() == true ? recommendedProduct.Reviews.Average(r => r.Rating) : 0;
            var maxRating = allProducts.Max(p => p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0);
            if (avgRating == maxRating && avgRating > 4)
            {
                reasons.Add($"⭐ Có đánh giá cao nhất ({avgRating:F1}/5 sao)");
            }

            // Brand reputation
            var premiumBrands = new[] { "Apple", "Dell", "HP", "Lenovo", "Asus", "MSI", "Razer", "Logitech" };
            if (premiumBrands.Contains(recommendedProduct.Brand, StringComparer.OrdinalIgnoreCase))
            {
                reasons.Add($"🏆 Thương hiệu uy tín ({recommendedProduct.Brand})");
            }

            // Category-specific reasons
            switch (comparisonType.ToLower())
            {
                case "laptop":
                    reasons.Add("💻 Cấu hình mạnh mẽ phù hợp cho công việc và giải trí");
                    break;
                case "mouse":
                    reasons.Add("🖱️ Hiệu năng chính xác cao, phù hợp cho gaming và văn phòng");
                    break;
                case "keyboard":
                    reasons.Add("⌨️ Trải nghiệm gõ tuyệt vời với switch chất lượng");
                    break;
                case "headphone":
                    reasons.Add("🎧 Chất lượng âm thanh vượt trội và thoải mái khi sử dụng");
                    break;
            }

            // Stock availability
            if (recommendedProduct.Stock > 0)
            {
                reasons.Add("✅ Còn hàng, có thể mua ngay");
            }

            return reasons;
        }

        private async Task<List<Product>> GetSuggestedProducts(int subSubcategoryId, List<int> excludeIds)
        {
            return await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Where(p => p.SubSubcategoryID == subSubcategoryId && 
                           !excludeIds.Contains(p.ProductID) && 
                           p.Status == "Active")
                .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0)
                .Take(8)
                .ToListAsync();
        }

        private Dictionary<string, List<TechnicalSpec>> GetTechnicalSpecsForComparison(List<Product> products, string comparisonType)
        {
            var result = new Dictionary<string, List<TechnicalSpec>>();
            
            if (!products.Any()) return result;

            // Get all product specifications
            var allSpecs = products.ToDictionary(p => p.ProductID, p => GetProductSpecifications(p));
            
            // Define specs by category
            var specsByCategory = GetSpecsByCategory(comparisonType);
            
            foreach (var category in specsByCategory)
            {
                var categorySpecs = new List<TechnicalSpec>();
                
                foreach (var specConfig in category.Value)
                {
                    var technicalSpec = new TechnicalSpec
                    {
                        Key = specConfig.Key,
                        DisplayName = specConfig.DisplayName,
                        Unit = specConfig.Unit,
                        HigherIsBetter = specConfig.HigherIsBetter
                    };
                    
                    // Get values for each product
                    var specValues = new List<SpecValue>();
                    foreach (var product in products)
                    {
                        var specs = allSpecs[product.ProductID];
                        var rawValue = specs.ContainsKey(specConfig.Key) ? specs[specConfig.Key]?.ToString() : "N/A";
                        var numericValue = ExtractNumericValue(rawValue);
                        
                        specValues.Add(new SpecValue
                        {
                            ProductId = product.ProductID,
                            RawValue = rawValue,
                            NumericValue = numericValue
                        });
                    }
                    
                    // Calculate highlighting classes
                    CalculateHighlightClasses(specValues, specConfig.HigherIsBetter);
                    technicalSpec.Values = specValues;
                    
                    // Add all specs, even with N/A values for debugging
                    categorySpecs.Add(technicalSpec);
                }
                
                // Always add category, even if empty for debugging
                result[category.Key] = categorySpecs;
            }
            
            // For monitors, always add raw attributes as fallback since they have many detailed specs
            var isMonitor = comparisonType?.ToLower() == "monitor";
            
            // Fallback: Add raw product attributes if no meaningful technical specs found OR if it's a monitor
            var hasSpecs = result.Values.Any(categorySpecs => 
                categorySpecs.Any(spec => 
                    spec.Values.Any(v => v.RawValue != "N/A" && !string.IsNullOrEmpty(v.RawValue))));
            
            if (!hasSpecs || isMonitor)
            {
                var rawSpecs = new List<TechnicalSpec>();
                
                // Get all unique attribute names
                var allAttributeNames = products
                    .SelectMany(p => p.ProductAttributeValues)
                    .Select(pav => new { 
                        Name = pav.AttributeValue.ProductAttribute.AttributeName,
                        Key = pav.AttributeValue.ProductAttribute.AttributeName.ToLower().Replace(" ", "_")
                    })
                    .GroupBy(x => x.Key)
                    .Select(g => g.First())
                    .ToList();
                
                foreach (var attr in allAttributeNames)
                {
                    var technicalSpec = new TechnicalSpec
                    {
                        Key = attr.Key,
                        DisplayName = attr.Name,
                        Unit = "",
                        HigherIsBetter = false
                    };
                    
                    var specValues = new List<SpecValue>();
                    foreach (var product in products)
                    {
                        var attrValue = product.ProductAttributeValues
                            .FirstOrDefault(pav => pav.AttributeValue.ProductAttribute.AttributeName == attr.Name);
                        
                        var rawValue = attrValue?.AttributeValue.ValueName ?? "N/A";
                        
                        specValues.Add(new SpecValue
                        {
                            ProductId = product.ProductID,
                            RawValue = rawValue,
                            NumericValue = ExtractNumericValue(rawValue),
                            HighlightClass = "equal"
                        });
                    }
                    
                    // Calculate highlighting for raw specs too
                    var isNumeric = specValues.Any(v => v.NumericValue > 0);
                    if (isNumeric)
                    {
                        CalculateHighlightClasses(specValues, true); // Default to higher is better
                    }
                    
                    technicalSpec.Values = specValues;
                    rawSpecs.Add(technicalSpec);
                }
                
                if (rawSpecs.Any())
                {
                    // For monitors, merge with existing categories instead of creating new one
                    if (isMonitor)
                    {
                        // Smart merge: Replace N/A specs with actual data, avoid duplicates
                        foreach (var rawSpec in rawSpecs)
                        {
                            var merged = false;
                            
                            foreach (var category in result.Keys.ToList())
                            {
                                var existingSpecs = result[category];
                                
                                // Check for similar specs that can be merged
                                var similarSpec = existingSpecs.FirstOrDefault(es => 
                                    es.Key == rawSpec.Key || 
                                    NormalizeSpecName(es.DisplayName) == NormalizeSpecName(rawSpec.DisplayName) ||
                                    AreSpecsSimilar(es.DisplayName, rawSpec.DisplayName));
                                
                                if (similarSpec != null)
                                {
                                    // Replace if existing has N/A values but raw has actual data
                                    var hasActualData = rawSpec.Values.Any(v => v.RawValue != "N/A" && !string.IsNullOrEmpty(v.RawValue));
                                    var existingHasNoData = similarSpec.Values.All(v => v.RawValue == "N/A" || string.IsNullOrEmpty(v.RawValue));
                                    
                                    if (hasActualData && existingHasNoData)
                                    {
                                        existingSpecs.Remove(similarSpec);
                                        existingSpecs.Add(rawSpec);
                                    }
                                    merged = true;
                                    break;
                                }
                            }
                            
                            // If not merged and has actual data, add to appropriate category
                            if (!merged)
                            {
                                var hasActualData = rawSpec.Values.Any(v => v.RawValue != "N/A" && !string.IsNullOrEmpty(v.RawValue));
                                if (hasActualData)
                                {
                                    // Categorize based on spec type
                                    var displaySpecs = new[] { "kích thước", "độ phân giải", "tần số quét", "tấm nền", "thời gian phản hồi", "size", "resolution", "refresh", "panel", "response" };
                                    var featureSpecs = new[] { "công nghệ đồng bộ", "cổng kết nối", "độ sáng", "tỉ lệ tương phản", "sync", "port", "brightness", "contrast" };
                                    
                                    var specLower = rawSpec.DisplayName.ToLower();
                                    
                                    if (displaySpecs.Any(ds => specLower.Contains(ds)) && result.ContainsKey("Hiển thị"))
                                    {
                                        result["Hiển thị"].Add(rawSpec);
                                    }
                                    else if (featureSpecs.Any(fs => specLower.Contains(fs)) && result.ContainsKey("Tính năng"))
                                    {
                                        result["Tính năng"].Add(rawSpec);
                                    }
                                    else if (result.Any())
                                    {
                                        result.First().Value.Add(rawSpec);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        result["Thông số sản phẩm"] = rawSpecs;
                    }
                }
            }
            
            return result;
        }

        private Dictionary<string, List<SpecConfig>> GetSpecsByCategory(string comparisonType)
        {
            var result = new Dictionary<string, List<SpecConfig>>();
            
            switch (comparisonType?.ToLower())
            {
                case "laptop":
                    result["Hiệu năng"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "cpu", DisplayName = "CPU", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "card_đồ_họa", DisplayName = "Card Đồ Họa", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "ram", DisplayName = "RAM", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "ổ_cứng", DisplayName = "Ổ Cứng", Unit = "", HigherIsBetter = true }
                    };
                    result["Màn hình"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "màn_hình", DisplayName = "Màn Hình", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "tần_số_quét", DisplayName = "Tần Số Quét", Unit = "", HigherIsBetter = true }
                    };
                    result["Hệ thống"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "hệ_điều_hành", DisplayName = "Hệ Điều Hành", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "trọng_lượng", DisplayName = "Trọng Lượng", Unit = "", HigherIsBetter = false }
                    };
                    break;
                    
                case "mouse":
                    result["Hiệu năng"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "cảm_biến", DisplayName = "Cảm biến", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "dpi", DisplayName = "DPI", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "polling_rate", DisplayName = "Polling Rate", Unit = "Hz", HigherIsBetter = true }
                    };
                    result["Thiết kế"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "kết_nối", DisplayName = "Kết nối", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "số_nút", DisplayName = "Số nút", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "trọng_lượng", DisplayName = "Trọng lượng", Unit = "", HigherIsBetter = false }
                    };
                    result["Tính năng"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "led", DisplayName = "LED", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "thời_lượng_pin", DisplayName = "Thời lượng pin", Unit = "", HigherIsBetter = true }
                    };
                    break;
                    
                case "keyboard":
                    result["Thiết kế"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "switch", DisplayName = "Switch", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "layout", DisplayName = "Layout", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "kiểu_bàn_phím", DisplayName = "Kiểu bàn phím", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "màu_sắc", DisplayName = "Màu sắc", Unit = "", HigherIsBetter = false }
                    };
                    result["Kết nối"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "kết_nối", DisplayName = "Kết nối", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "pin", DisplayName = "Pin", Unit = "", HigherIsBetter = true }
                    };
                    break;
                    
                case "pc":
                case "gaming pc":
                case "pc gaming":
                    result["Hiệu năng"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "cpu", DisplayName = "CPU", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "vga", DisplayName = "VGA", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "ram", DisplayName = "RAM", Unit = "", HigherIsBetter = true }
                    };
                    result["Linh kiện"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "mainboard", DisplayName = "Mainboard", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "ổ_cứng", DisplayName = "Ổ cứng", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "psu", DisplayName = "PSU", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "case", DisplayName = "Case", Unit = "", HigherIsBetter = false }
                    };
                    break;
                    
                case "monitor":
                    result["Hiển thị"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "screen_size", DisplayName = "Kích thước", Unit = "inch", HigherIsBetter = true },
                        new SpecConfig { Key = "resolution", DisplayName = "Độ phân giải", Unit = "", HigherIsBetter = true },
                        new SpecConfig { Key = "refresh_rate_hz", DisplayName = "Tần số quét", Unit = "Hz", HigherIsBetter = true },
                        new SpecConfig { Key = "panel_type", DisplayName = "Tấm nền", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "response_time", DisplayName = "Thời gian phản hồi", Unit = "ms", HigherIsBetter = false }
                    };
                    result["Tính năng"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "sync_technology", DisplayName = "Công nghệ đồng bộ", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "connection_ports", DisplayName = "Cổng kết nối", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "brightness", DisplayName = "Độ sáng", Unit = "nits", HigherIsBetter = true },
                        new SpecConfig { Key = "contrast_ratio", DisplayName = "Tỉ lệ tương phản", Unit = "", HigherIsBetter = true }
                    };
                    break;
                    
                default:
                    result["Thông số kỹ thuật"] = new List<SpecConfig>
                    {
                        new SpecConfig { Key = "brand", DisplayName = "Thương hiệu", Unit = "", HigherIsBetter = false },
                        new SpecConfig { Key = "warranty", DisplayName = "Bảo hành", Unit = "", HigherIsBetter = true }
                    };
                    break;
            }
            
            return result;
        }

        private void CalculateHighlightClasses(List<SpecValue> values, bool higherIsBetter)
        {
            var numericValues = values.Where(v => v.NumericValue > 0).ToList();
            if (numericValues.Count <= 1) 
            {
                foreach (var value in values)
                {
                    value.HighlightClass = "equal";
                }
                return;
            }
            
            var maxValue = numericValues.Max(v => v.NumericValue);
            var minValue = numericValues.Min(v => v.NumericValue);
            
            foreach (var value in values)
            {
                if (value.NumericValue <= 0 || value.RawValue == "N/A")
                {
                    value.HighlightClass = "equal";
                }
                else if (Math.Abs(value.NumericValue - maxValue) < 0.01)
                {
                    value.HighlightClass = higherIsBetter ? "best" : "worst";
                }
                else if (Math.Abs(value.NumericValue - minValue) < 0.01)
                {
                    value.HighlightClass = higherIsBetter ? "worst" : "best";
                }
                else
                {
                    value.HighlightClass = "equal";
                }
            }
        }

        private string NormalizeSpecName(string specName)
        {
            return specName?.ToLower()
                .Replace(" ", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("_", "")
                .Replace("-", "") ?? "";
        }
        
        private bool AreSpecsSimilar(string spec1, string spec2)
        {
            var similarPairs = new Dictionary<string, string[]>
            {
                ["kích thước"] = new[] { "size", "screen size", "kích thước" },
                ["độ phân giải"] = new[] { "resolution", "độ phân giải" },
                ["tần số quét"] = new[] { "refresh rate", "refresh", "tần số quét", "tần số" },
                ["tấm nền"] = new[] { "panel", "panel type", "tấm nền" },
                ["thời gian phản hồi"] = new[] { "response time", "response", "thời gian phản hồi" },
                ["công nghệ đồng bộ"] = new[] { "sync technology", "sync", "công nghệ đồng bộ", "đồng bộ" },
                ["cổng kết nối"] = new[] { "connection ports", "ports", "connectivity", "cổng kết nối", "kết nối" },
                ["độ sáng"] = new[] { "brightness", "độ sáng" },
                ["tỉ lệ tương phản"] = new[] { "contrast ratio", "contrast", "tỉ lệ tương phản", "tương phản" }
            };
            
            var norm1 = NormalizeSpecName(spec1);
            var norm2 = NormalizeSpecName(spec2);
            
            foreach (var pair in similarPairs)
            {
                var normalizedGroup = pair.Value.Select(s => NormalizeSpecName(s)).ToArray();
                if (normalizedGroup.Contains(norm1) && normalizedGroup.Contains(norm2))
                {
                    return true;
                }
            }
            
            return false;
        }

        private class SpecConfig
        {
            public string Key { get; set; }
            public string DisplayName { get; set; }
            public string Unit { get; set; }
            public bool HigherIsBetter { get; set; }
        }
    }
} 