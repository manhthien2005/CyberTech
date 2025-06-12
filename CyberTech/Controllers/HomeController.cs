using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CyberTech.Data;
using CyberTech.Helpers;
using CyberTech.Models;

namespace CyberTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index(int page = 1, int pageSize = 10)
        {
            // Lấy danh sách sản phẩm với phân trang và kiểm tra dữ liệu đầy đủ
            var products = GetOptimizedProductQuery()
                .Where(p => p.SubSubcategory != null &&
                           p.SubSubcategory.Subcategory != null &&
                           p.SubSubcategory.Subcategory.Category != null)
                .OrderBy(p => p.ProductID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Lấy danh sách thuộc tính quan trọng theo danh mục
            var categoryAttributes = _context.CategoryAttributes
                .GroupBy(ca => ca.CategoryID)
                .ToDictionary(g => g.Key, g => g.Select(ca => ca.AttributeName).ToList());

            // Tạo ProductViewModel
            var productViewModels = products.Select(p => new ProductViewModel
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Price = p.Price,
                SalePrice = p.SalePrice,
                SalePercentage = p.SalePercentage,
                DiscountedPrice = p.SalePrice,
                Attributes = GetImportantAttributes(p, categoryAttributes),
                SubSubcategory = p.SubSubcategory,
                PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                IsInStock = p.Stock > 0,
                Brand = p.Brand,
                AverageRating = p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews?.Count ?? 0,
                Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                Status = p.Status ?? "Active"
            }).ToList();

            // Lấy danh sách danh mục với phân cấp - Optimized với AsSplitQuery()
            var categories = _context.Categories
                .AsSplitQuery()
                .Include(c => c.Subcategories)
                .ThenInclude(s => s.SubSubcategories)
                .Select(c => new CategoryViewModel
                {
                    CategoryID = c.CategoryID,
                    Name = c.Name,
                    Icon = CategoryHelper.GetIconForCategory(c.Name),
                    Url = Url.Action("Category", "Product", new { category = c.Name.ToLower().Replace(" ", "-") }),
                    Subcategories = c.Subcategories.Select(s => new SubcategoryViewModel
                    {
                        SubcategoryID = s.SubcategoryID,
                        Name = s.Name,
                        Url = Url.Action("Subcategory", "Product", new { subcategory = s.Name.ToLower().Replace(" ", "-") }),
                        SubSubcategories = s.SubSubcategories.Select(ss => new SubSubcategoryViewModel
                        {
                            SubSubcategoryID = ss.SubSubcategoryID,
                            Name = ss.Name,
                            Url = Url.Action("SubSubcategory", "Product", new { subSubcategory = ss.Name.ToLower().Replace(" ", "-") })
                        }).ToList()
                    }).ToList()
                })
                .ToList();

            // Lấy sản phẩm Laptop Gaming bán chạy
            var laptopGamingBestSellers = GetOptimizedProductQuery()
                .Where(p => p.SubSubcategory.Subcategory.CategoryID == 2)
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            // Lấy sản phẩm Laptop Văn Phòng bán chạy
            var laptopOfficeBestSellers = GetOptimizedProductQuery()
                .Where(p => p.SubSubcategory.Subcategory.CategoryID == 1)
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            // Lấy sản phẩm PC Gaming bán chạy
            var pcGamingBestSellers = GetOptimizedProductQuery()
                .Where(p => p.SubSubcategory.Subcategory.CategoryID == 3)
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            // Lấy sản phẩm Chuột bán chạy
            var mouseBestSellers = GetOptimizedProductQuery()
                .Where(p => p.SubSubcategory.Subcategory.CategoryID == 10)
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            // Lấy sản phẩm Màn hình bán chạy
            var monitorBestSellers = GetOptimizedProductQuery()
                .Where(p => p.SubSubcategory.Subcategory.CategoryID == 8)
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            // Lấy sản phẩm Bàn phím bán chạy
            var keyboardBestSellers = GetOptimizedProductQuery()
                .Where(p => p.SubSubcategory.Subcategory.CategoryID == 9)
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            // Lấy sản phẩm Flash Sale (có discount)
            var flashSaleProducts = GetOptimizedProductQuery()
                .Where(p => (p.SalePrice.HasValue && p.SalePrice < p.Price) || 
                           (p.SalePercentage.HasValue && p.SalePercentage > 0))
                .OrderByDescending(p => p.SalePercentage ?? ((p.Price - (p.SalePrice ?? p.Price)) / p.Price * 100)) // Sort by discount percentage
                .Take(15) // Lấy 15 sản phẩm flash sale
                .ToList();

            _logger.LogInformation("Flash Sale products: {0}", string.Join(", ", flashSaleProducts.Select(p => p.Name)));
            _logger.LogInformation("Laptop Gaming bán chạy: {0}", string.Join(", ", laptopGamingBestSellers.Select(p => p.Name)));
            _logger.LogInformation("Laptop Văn Phòng bán chạy: {0}", string.Join(", ", laptopOfficeBestSellers.Select(p => p.Name)));
            _logger.LogInformation("PC Gaming bán chạy: {0}", string.Join(", ", pcGamingBestSellers.Select(p => p.Name)));
            _logger.LogInformation("Chuột bán chạy: {0}", string.Join(", ", mouseBestSellers.Select(p => p.Name)));
            _logger.LogInformation("Màn hình bán chạy: {0}", string.Join(", ", monitorBestSellers.Select(p => p.Name)));
            _logger.LogInformation("Bàn phím bán chạy: {0}", string.Join(", ", keyboardBestSellers.Select(p => p.Name)));

            var viewModel = new HomeIndexViewModel
            {
                Products = productViewModels,
                Categories = categories,
                FlashSaleProducts = flashSaleProducts.Select(p => new ProductViewModel
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    SalePercentage = p.SalePercentage,
                    DiscountedPrice = p.SalePrice,
                    Attributes = GetImportantAttributes(p, categoryAttributes),
                    SubSubcategory = p.SubSubcategory,
                    PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    IsInStock = p.Stock > 0,
                    Brand = p.Brand,
                    AverageRating = p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews?.Count ?? 0,
                    Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                    Status = p.Status ?? "Active"
                }).ToList(),
                LaptopGamingBestSellers = laptopGamingBestSellers.Select(p => new ProductViewModel
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    SalePercentage = p.SalePercentage,
                    DiscountedPrice = p.SalePrice,
                    Attributes = GetImportantAttributes(p, categoryAttributes),
                    SubSubcategory = p.SubSubcategory,
                    PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    IsInStock = p.Stock > 0,
                    Brand = p.Brand,
                    AverageRating = p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews?.Count ?? 0,
                    Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                    Status = p.Status ?? "Active"
                }).ToList(),
                LaptopOfficeBestSellers = laptopOfficeBestSellers.Select(p => new ProductViewModel
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    SalePercentage = p.SalePercentage,
                    DiscountedPrice = p.SalePrice,
                    Attributes = GetImportantAttributes(p, categoryAttributes),
                    SubSubcategory = p.SubSubcategory,
                    PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    IsInStock = p.Stock > 0,
                    Brand = p.Brand,
                    AverageRating = p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews?.Count ?? 0,
                    Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                    Status = p.Status ?? "Active"
                }).ToList(),
                PcGamingBestSellers = pcGamingBestSellers.Select(p => new ProductViewModel
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    SalePercentage = p.SalePercentage,
                    DiscountedPrice = p.SalePrice,
                    Attributes = GetImportantAttributes(p, categoryAttributes),
                    SubSubcategory = p.SubSubcategory,
                    PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    IsInStock = p.Stock > 0,
                    Brand = p.Brand,
                    AverageRating = p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews?.Count ?? 0,
                    Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                    Status = p.Status ?? "Active"
                }).ToList(),
                MouseBestSellers = mouseBestSellers.Select(p => new ProductViewModel
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    SalePercentage = p.SalePercentage,
                    DiscountedPrice = p.SalePrice,
                    Attributes = GetImportantAttributes(p, categoryAttributes),
                    SubSubcategory = p.SubSubcategory,
                    PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    IsInStock = p.Stock > 0,
                    Brand = p.Brand,
                    AverageRating = p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews?.Count ?? 0,
                    Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                    Status = p.Status ?? "Active"
                }).ToList(),
                MonitorBestSellers = monitorBestSellers.Select(p => new ProductViewModel
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    SalePercentage = p.SalePercentage,
                    DiscountedPrice = p.SalePrice,
                    Attributes = GetImportantAttributes(p, categoryAttributes),
                    SubSubcategory = p.SubSubcategory,
                    PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    IsInStock = p.Stock > 0,
                    Brand = p.Brand,
                    AverageRating = p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews?.Count ?? 0,
                    Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                    Status = p.Status ?? "Active"
                }).ToList(),
                KeyboardBestSellers = keyboardBestSellers.Select(p => new ProductViewModel
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    SalePercentage = p.SalePercentage,
                    DiscountedPrice = p.SalePrice,
                    Attributes = GetImportantAttributes(p, categoryAttributes),
                    SubSubcategory = p.SubSubcategory,
                    PrimaryImageUrl = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    PrimaryImageUrlSmall = p.ProductImages.FirstOrDefault()?.ImageURL ?? "/placeholder.svg",
                    IsInStock = p.Stock > 0,
                    Brand = p.Brand,
                    AverageRating = p.Reviews?.Any() == true ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews?.Count ?? 0,
                    Url = Url.Action("ProductDetail", "Product", new { id = p.ProductID }),
                    Status = p.Status ?? "Active"
                }).ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Helper method để tạo optimized query cho products với split query
        /// </summary>
        private IQueryable<Product> GetOptimizedProductQuery()
        {
            return _context.Products
                .AsSplitQuery()
                .Include(p => p.SubSubcategory)
                .ThenInclude(ss => ss.Subcategory)
                .ThenInclude(s => s.Category)
                .Include(p => p.ProductAttributeValues)
                .ThenInclude(pav => pav.AttributeValue)
                .ThenInclude(av => av.ProductAttribute)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews);
        }

        private Dictionary<string, string> GetImportantAttributes(Product product, Dictionary<int, List<string>> categoryAttributes)
        {
            if (product.SubSubcategory?.Subcategory?.Category == null)
            {
                _logger.LogWarning($"Product {product.ProductID} lacks complete category hierarchy.");
                return new Dictionary<string, string>();
            }

            var categoryId = product.SubSubcategory.Subcategory.Category.CategoryID;
            if (!categoryAttributes.TryGetValue(categoryId, out var importantAttributes))
            {
                return new Dictionary<string, string>();
            }

            return product.ProductAttributeValues
                .Where(pav => pav.AttributeValue?.ProductAttribute != null &&
                             importantAttributes.Contains(pav.AttributeValue.ProductAttribute.AttributeName))
                .OrderBy(pav => importantAttributes.IndexOf(pav.AttributeValue.ProductAttribute.AttributeName))
                .ToDictionary(
                    pav => pav.AttributeValue.ProductAttribute.AttributeName,
                    pav => pav.AttributeValue.ValueName
                );
        }

        public IActionResult Product()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}