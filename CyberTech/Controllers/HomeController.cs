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
        private readonly IWebHostEnvironment _environment;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            try
            {
                // Thêm logging để kiểm tra khi Railway gọi healthcheck
                _logger.LogInformation("Home/Index endpoint called at {Time}", DateTime.Now);

                // Lấy danh sách sản phẩm nổi bật và mới nhất
                var featuredProducts = _context.Products
                    .Where(p => p.IsFeatured && p.IsActive && p.Stock > 0)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToList();

                var newProducts = _context.Products
                    .Where(p => p.IsActive && p.Stock > 0)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToList();

                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = featuredProducts,
                    NewProducts = newProducts
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Home/Index action");
                
                // Trong môi trường production, trả về view đơn giản cho healthcheck
                if (_environment.IsProduction())
                {
                    return Content("Application is running");
                }
                
                // Trong môi trường development, hiển thị lỗi
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
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