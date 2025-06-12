/*
 * Copyright (c) 2025 Lê Anh Tuấn and Phan Điền Mạnh Thiên
 * Trường Đại học tư thục quốc tế sài gòn SIU
 * All rights reserved.
 */

using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Services;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Linq;

namespace CyberTech.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IAntiforgery _antiforgery;
        private readonly IChatLogService _chatLogService;

        public ChatController(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IAntiforgery antiforgery,
            IChatLogService chatLogService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _antiforgery = antiforgery;
            _chatLogService = chatLogService;
        }

        [ValidateAntiForgeryToken]
        [HttpPost("GeminiChat")]
        public async Task<IActionResult> GeminiChat([FromBody] ChatRequest request)
        {
            // ✅ **INPUT VALIDATION**
            if (string.IsNullOrWhiteSpace(request.UserInput))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập nội dung tin nhắn" });
            }

            string replyText = null;

            try
            {
                // 🤖 **SYSTEM PROMPT CONFIGURATION**
                var systemPrompt = @"🤖 **CyberTech AI Assistant**

🏪 **Giới thiệu**: Tôi là Cybot, trợ lý thông minh của CyberTech, chuyên tư vấn về laptop, PC gaming, bàn phím cơ, màn hình, chuột và phụ kiện. Với dữ liệu sản phẩm từ cơ sở dữ liệu, tôi mang đến thông tin chính xác, thân thiện và chuyên nghiệp để hỗ trợ bạn chọn sản phẩm phù hợp.

📝 **Định dạng câu trả lời** (BẮT BUỘC tuân thủ):
- Các đề mục phải in đậm chữ to: ví dụ **Thông tin chi tiết về sản phẩm**, **Thông số kỹ thuật nổi bật**...

- **🏷️ Tên sản phẩm**: In đậm, kèm đường link sản phẩm. Ví dụ:  
  **[Laptop ASUS ROG Zephyrus](URL)**

- **🖼️ Hình ảnh sản phẩm**: Hiển thị bằng cú pháp Markdown. Ví dụ:  
  ![Laptop ASUS ROG](URL_hình_ảnh)

- **📋 Danh sách sản phẩm**: Sử dụng bullet points (`-`), mỗi sản phẩm trên một dòng riêng. Ví dụ:  
  - **[Laptop ASUS ROG Zephyrus](URL)** (ID: 123)  
  - **[Laptop Dell XPS 13](URL)** (ID: 124)

- **💰 Giá và khuyến mãi**: In nghiêng cho giá gốc, in đậm cho giá ưu đãi, mỗi thông tin trên một dòng riêng. Ví dụ:  
  - **Giá gốc:** *1,000,000đ*  
  - **Giảm giá:** 10% → ***Giá ưu đãi***: *900,000đ*

- **⚙️ Thông số kỹ thuật**: Liệt kê bằng bullet points, mỗi thông số trên một dòng riêng. Key thì in đậm ví dụ **CPU**. Ví dụ:  
  - **CPU:** Intel Core i7  
  - **RAM:** 16GB  
  - **Ổ cứng:** 512GB SSD

- **🎫 Voucher**: Liệt kê mã, giá trị, điều kiện, mỗi voucher trên một dòng riêng. Ví dụ:  
  - **VOUCHER10**: Giảm 10%, áp dụng cho laptop, còn 50 lượt  
  - **FREESHIP**: Miễn phí vận chuyển, đơn từ 500,000đ

- **📊 So sánh sản phẩm**: Sử dụng bảng Markdown, mỗi hàng trên một dòng riêng. Ví dụ:  
  | Sản phẩm                  | Giá          | CPU         |  
  |---------------------------|--------------|-------------|  
  | **[ASUS ROG](URL)**       | *900,000đ*   | Intel i7    |  
  | **[Dell XPS](URL)**       | *950,000đ*   | Intel i5    |

- **⭐ Đánh giá sản phẩm**: Hiển thị số sao, điểm trung bình, và đánh giá gần đây. Ví dụ:  
  - ⭐⭐⭐⭐⭐ 4.8/5 (từ 24 đánh giá)  
  - Đánh giá gần đây: 'Sản phẩm rất tốt, đáng tiền' - Nguyễn Văn A

🔒 **Quy tắc**:
- KHÔNG tiết lộ mã nguồn, mật khẩu, hoặc thông tin cấu hình hệ thống.
- Nếu không có thông tin, trả lời:  
  ❌ Thông tin không khả dụng. Vui lòng liên hệ hỗ trợ qua hotline 0977223517.
- BẮT BUỘC sử dụng Markdown và tuân thủ nghiêm ngặt định dạng trên cho TẤT CẢ câu trả lời, không bỏ qua bất kỳ phần nào.
- Mỗi thông tin (bullet point, thông số, giá, voucher, v.v.) PHẢI được trình bày trên một dòng riêng để đảm bảo dễ đọc. KHÔNG ghi các thông tin trên cùng một dòng.
- Trả lời tự nhiên, thân thiện như một nhân viên tư vấn chuyên nghiệp, sử dụng ngôn ngữ gần gũi, dễ hiểu.
- Nhấn mạnh ưu điểm và lợi ích của sản phẩm, phù hợp với nhu cầu của khách hàng.
- Sử dụng các từ ngữ thân thiện như 'bạn', 'quý khách', 'anh/chị' khi giao tiếp. KHÔNG được chửi thề với khách hàng.
- Kết thúc câu trả lời bằng lời mời mua sắm hoặc khuyến khích khách hàng hỏi thêm thông tin, ví dụ:  
  'Bạn cần thêm thông tin hay muốn đặt hàng ngay? Liên hệ mình nhé!'
- Sử dụng lịch sử hội thoại (nếu có) để trả lời các câu hỏi liên quan đến ngữ cảnh trước đó. Ví dụ, nếu người dùng hỏi 'Cái laptop đó còn khuyến mãi không?', hãy tham khảo các sản phẩm đã đề cập trước đó để trả lời chính xác.";

                // 📦 **DATABASE QUERY - PRODUCTS**
                var products = _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                        .ThenInclude(r => r.User)
                    .Join(_context.SubSubcategories,
                        p => p.SubSubcategoryID,
                        ssc => ssc.SubSubcategoryID,
                        (p, ssc) => new { p, ssc })
                    .Join(_context.Subcategories,
                        ps => ps.ssc.SubcategoryID,
                        sc => sc.SubcategoryID,
                        (ps, sc) => new { ps.p, ps.ssc, sc })
                    .Join(_context.Categories,
                        psc => psc.sc.CategoryID,
                        c => c.CategoryID,
                        (psc, c) => new
                        {
                            psc.p.ProductID,
                            psc.p.Name,
                            psc.p.Description,
                            psc.p.Price,
                            psc.p.SalePercentage,
                            psc.p.SalePrice,
                            psc.p.Stock,
                            psc.p.Brand,
                            CategoryName = c.Name,
                            SubcategoryName = psc.sc.Name,
                            SubSubcategoryName = psc.ssc.Name,
                            ProductImages = psc.p.ProductImages,
                            Reviews = psc.p.Reviews
                        })
                    .ToList();

                // ⚙️ **DATABASE QUERY - PRODUCT ATTRIBUTES**
                var productAttributes = _context.ProductAttributeValues
                    .Join(_context.AttributeValues,
                        pav => pav.ValueID,
                        av => av.ValueID,
                        (pav, av) => new { pav.ProductID, av.AttributeID, av.ValueName })
                    .Join(_context.ProductAttributes,
                        pav => pav.AttributeID,
                        pa => pa.AttributeID,
                        (pav, pa) => new { pav.ProductID, AttributeName = pa.AttributeName, pav.ValueName })
                    .ToList();

                // 🎫 **DATABASE QUERY - SYSTEM VOUCHERS**
                var vouchers = _context.Vouchers
                    .Where(v => v.IsActive && v.ValidFrom <= DateTime.Now && v.ValidTo >= DateTime.Now && v.IsSystemWide)
                    .Select(v => new
                    {
                        v.Code,
                        v.Description,
                        v.DiscountType,
                        v.DiscountValue,
                        v.QuantityAvailable,
                        v.AppliesTo
                    })
                    .ToList();

                // 🏗️ **BUILD PRODUCT INFORMATION**
                var productInfo = new StringBuilder();
                foreach (var product in products)
                {
                    // Tạo URL sản phẩm
                    var productUrl = Url.Action("ProductDetail", "Product", new { id = product.ProductID }, Request.Scheme);

                    // Lấy hình ảnh sản phẩm
                    var primaryImage = product.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL ??
                                      product.ProductImages.FirstOrDefault()?.ImageURL ??
                                      "/images/no-image.png";

                    // Resolve absolute or relative image URL
                    var imageUrl = primaryImage.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? primaryImage
                        : $"{Request.Scheme}://{Request.Host}{primaryImage}";

                    // Tính điểm đánh giá trung bình
                    double averageRating = 0;
                    if (product.Reviews != null && product.Reviews.Any())
                    {
                        averageRating = product.Reviews.Average(r => r.Rating);
                    }

                    // Hiển thị sản phẩm với link và hình ảnh
                    productInfo.AppendLine($"• **[{product.Name}]({productUrl})** (ID: {product.ProductID})");
                    productInfo.AppendLine($"  ![{product.Name}]({imageUrl})");

                    // 📂 **Danh mục dạng list**
                    productInfo.AppendLine($"  📂 **Danh mục**:");
                    productInfo.AppendLine($"    - Loại: {product.CategoryName}");
                    productInfo.AppendLine($"    - Phân loại: {product.SubcategoryName}");
                    productInfo.AppendLine($"    - Chi tiết: {product.SubSubcategoryName}");

                    // 📝 **Mô tả dạng list**
                    if (!string.IsNullOrEmpty(product.Description))
                    {
                        productInfo.AppendLine($"  📝 **Mô tả**:");
                        var descriptionLines = product.Description.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in descriptionLines.Take(3))
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                productInfo.AppendLine($"    - {line.Trim()}");
                        }
                    }

                    productInfo.AppendLine($"  💰 **Giá gốc**: *{product.Price:N0}đ*");

                    if (product.SalePercentage > 0)
                    {
                        productInfo.AppendLine($"  🔥 **Giảm giá**: {product.SalePercentage}% → **Giá ưu đãi**: *{product.SalePrice:N0}đ*");
                    }

                    productInfo.AppendLine($"  📦 **Tồn kho**: {product.Stock}");
                    productInfo.AppendLine($"  🏭 **Thương hiệu**: {product.Brand}");

                    // ⭐ **Đánh giá sản phẩm**
                    if (product.Reviews != null && product.Reviews.Any())
                    {
                        var starRating = new string('⭐', (int)Math.Round(averageRating));
                        productInfo.AppendLine($"  ⭐ **Đánh giá**: {starRating} {averageRating:F1}/5 (từ {product.Reviews.Count()} đánh giá)");

                        // Hiển thị đánh giá gần đây nhất
                        var recentReview = product.Reviews.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
                        if (recentReview != null)
                        {
                            productInfo.AppendLine($"    - Đánh giá gần đây: \"{recentReview.Comment}\" - {recentReview.User?.Name ?? "Khách hàng"}");
                        }
                    }
                    else
                    {
                        productInfo.AppendLine($"  ⭐ **Đánh giá**: Chưa có đánh giá");
                    }

                    // ⚙️ **Thông số kỹ thuật dạng list**
                    var attributes = productAttributes
                        .Where(pa => pa.ProductID == product.ProductID)
                        .Select(pa => $"{pa.AttributeName}: {pa.ValueName}");

                    if (attributes.Any())
                    {
                        productInfo.AppendLine($"  ⚙️ **Thông số kỹ thuật**:");
                        foreach (var attr in attributes)
                        {
                            productInfo.AppendLine($"    - {attr}");
                        }
                    }
                    productInfo.AppendLine();
                }

                // 🎫 **BUILD VOUCHER INFORMATION**
                var voucherInfo = string.Join("\n", vouchers.Select(v =>
                    $"🎫 **{v.Code}**: {v.Description} " +
                    $"(💸 Giảm {(v.DiscountType == "PERCENT" ? $"{v.DiscountValue}%" : $"{v.DiscountValue:N0}đ")}" +
                    $", 🎯 Áp dụng cho: {v.AppliesTo}, 📊 Số lượng: {(v.QuantityAvailable.HasValue ? v.QuantityAvailable : "Không giới hạn")})"));

                // 📚 **BUILD CATEGORY INFORMATION**
                var categoryInfo = string.Join(", ", _context.Categories.Select(c => c.Name));
                var subcategoryInfo = string.Join(", ", _context.Subcategories.Select(sc => sc.Name));

                // 🔗 **COMBINE CONTEXT DATA**
                var fullContext = $@"
                📋 **DANH MỤC SẢN PHẨM**: {categoryInfo}

                📂 **DANH MỤC PHỤ**: {subcategoryInfo}

                🛍️ **DANH SÁCH SẢN PHẨM**:
                {productInfo}

                🎫 **VOUCHER HỆ THỐNG**:
                {voucherInfo}";

                // 🗣️ **BUILD CONVERSATION HISTORY**
                var conversationHistory = new StringBuilder();
                if (request.ConversationHistory != null && request.ConversationHistory.Any())
                {
                    conversationHistory.AppendLine("🗣️ **LỊCH SỬ HỘI THOẠI**:");
                    foreach (var message in request.ConversationHistory)
                    {
                        var role = message.Role == "user" ? "Người dùng" : "Cybot";
                        conversationHistory.AppendLine($"- **{role}**: {message.Content}");
                    }
                    conversationHistory.AppendLine();
                }

                // 📝 **COMBINE FULL PROMPT**
                var fullPrompt = $@"{systemPrompt}

                📊 **THÔNG TIN CƠ SỞ DỮ LIỆU**:
                {fullContext}

                {conversationHistory}

                ❓ **CÂU HỎI NGƯỜI DÙNG**: {request.UserInput}";

                // 🌐 **GEMINI API CALL**
                var httpClient = _httpClientFactory.CreateClient();
                var apiKey = _configuration["GeminiSettings:ApiKey"];
                var apiEndpoint = $"{_configuration["GeminiSettings:ApiEndpoint"]}?key={apiKey}";

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

                var response = await httpClient.PostAsJsonAsync(apiEndpoint, requestBody);

                // ❌ **CHECK API RESPONSE**
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        message = "❌ Không thể kết nối với dịch vụ chat. Vui lòng thử lại sau."
                    });
                }

                // 📝 **PARSE GEMINI RESPONSE**
                var responseBody = await response.Content.ReadFromJsonAsync<JsonElement>();
                replyText = responseBody
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrEmpty(replyText))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "❌ Không nhận được phản hồi từ chatbot"
                    });
                }

                // 🔄 **MARKDOWN TO HTML CONVERSION**
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();
                string htmlResponse = Markdown.ToHtml(replyText, pipeline);

                // ✅ **SUCCESS RESPONSE**
                return Ok(new
                {
                    success = true,
                    html = htmlResponse,
                    message = replyText
                });
            }
            catch (Exception ex)
            {
                // ❌ **ERROR HANDLING**
                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ Đã có lỗi xảy ra. Vui lòng thử lại sau."
                });
            }
            finally
            {
                // Log the chat conversation regardless of success or failure
                try
                {
                    // Get user email if authenticated
                    string userEmail = null;
                    if (User.Identity.IsAuthenticated)
                    {
                        userEmail = User.FindFirstValue(ClaimTypes.Email);
                    }
                    else
                    {
                        // Use session ID for anonymous users
                        userEmail = HttpContext.Session.Id;
                    }

                    // Log the chat asynchronously without waiting
                    _ = _chatLogService.LogChatAsync(
                        request.UserInput,
                        replyText ?? "Error: No response generated",
                        userEmail
                    );
                }
                catch (Exception logEx)
                {
                    // Just log the error but don't affect the response
                    Console.Error.WriteLine($"Error logging chat: {logEx.Message}");
                }
            }
        }

        // 🎨 **CHAT WIDGET ENDPOINT**
        [HttpGet("widget")]
        public IActionResult GetChatWidget()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewBag.AntiforgeryToken = tokens.RequestToken;
            return PartialView("_ChatWidget");
        }

        // 📊 **ADMIN CHAT LOGS ENDPOINT**
        [HttpGet("logs/{userIdentifier}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ViewChatLogs(string userIdentifier, int maxEntries = 100)
        {
            try
            {
                var logs = await _chatLogService.GetChatHistoryAsync(userIdentifier, maxEntries);
                return View("ChatLogs", logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error retrieving chat logs: " + ex.Message });
            }
        }

        // 📂 **LIST ALL CHAT LOG FILES**
        [HttpGet("logs")]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult ListChatLogs()
        {
            try
            {
                string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "ChatLogs");
                if (!Directory.Exists(logDirectory))
                {
                    return View("ChatLogsList", new List<string>());
                }

                var logFiles = Directory.GetFiles(logDirectory, "chat_log_*.json")
                    .Select(Path.GetFileName)
                    .OrderByDescending(f => f)
                    .ToList();

                return View("ChatLogsList", logFiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error listing chat logs: " + ex.Message });
            }
        }
    }

    // 📋 **DATA MODELS**
    public class ChatRequest
    {
        public string UserInput { get; set; }
        public List<ChatMessage> ConversationHistory { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; } // "user" or "bot"
        public string Content { get; set; }
    }

    public class AIResponse
    {
        public string Reply { get; set; }
    }
}