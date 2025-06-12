using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using CyberTech.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;

namespace CyberTechShop.Controllers
{
    [Authorize(Roles = "Admin,Staff,Support,Manager,SuperAdmin")]
    public class CategoryManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public CategoryManageController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region API Endpoints

        /// <summary>
        /// Lấy toàn bộ cây danh mục với thống kê - TỐI ƯU ĐƠN GIẢN
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategoriesTree()
        {
            try
            {
                // Kiểm tra cache trước
                const string cacheKey = "categories_tree";
                if (_cache.TryGetValue(cacheKey, out var cachedResult))
                {
                    return Json(new { success = true, data = cachedResult, cached = true });
                }

                // Load data đơn giản với AsNoTracking
                var categories = await _context.Categories
                    .Include(c => c.Subcategories)
                        .ThenInclude(sc => sc.SubSubcategories)
                    .AsNoTracking()
                    .ToListAsync();

                // Đếm products một lần cho tất cả
                var productCounts = await _context.Products
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => new { 
                        CategoryId = p.SubSubcategory.Subcategory.CategoryID,
                        SubcategoryId = p.SubSubcategory.SubcategoryID,
                        SubSubcategoryId = p.SubSubcategoryID
                    })
                    .Select(g => new {
                        CategoryId = g.Key.CategoryId,
                        SubcategoryId = g.Key.SubcategoryId, 
                        SubSubcategoryId = g.Key.SubSubcategoryId,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Tạo dictionary cho lookup nhanh
                var subSubCategoryCounts = productCounts.ToDictionary(x => x.SubSubcategoryId, x => x.Count);
                var subcategoryCounts = productCounts.GroupBy(x => x.SubcategoryId).ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
                var categoryCounts = productCounts.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

                // Build response
                var result = categories.Select(c => new
                {
                    id = c.CategoryID,
                    name = c.Name,
                    description = c.Description,
                    productCount = categoryCounts.GetValueOrDefault(c.CategoryID, 0),
                    subcategories = c.Subcategories.Select(sc => new
                    {
                        id = sc.SubcategoryID,
                        name = sc.Name,
                        description = sc.Description,
                        productCount = subcategoryCounts.GetValueOrDefault(sc.SubcategoryID, 0),
                        subSubcategories = sc.SubSubcategories.Select(ssc => new
                        {
                            id = ssc.SubSubcategoryID,
                            name = ssc.Name,
                            description = ssc.Description,
                            productCount = subSubCategoryCounts.GetValueOrDefault(ssc.SubSubcategoryID, 0)
                        }).ToList()
                    }).ToList()
                }).ToList();

                // Cache kết quả 5 phút
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê tổng quan - ĐƠN GIẢN
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                // Kiểm tra cache trước
                const string cacheKey = "category_statistics";
                if (_cache.TryGetValue(cacheKey, out var cachedStats))
                {
                    return Json(new { success = true, data = cachedStats, cached = true });
                }

                // Đơn giản: 4 queries bình thường
                var result = new
                {
                    mainCategoryCount = await _context.Categories.CountAsync(),
                    subCategoryCount = await _context.Subcategories.CountAsync(),
                    subSubCategoryCount = await _context.SubSubcategories.CountAsync(),
                    productCount = await _context.Products.CountAsync(p => p.Status == "Active")
                };

                // Cache 5 phút
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo danh mục mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                switch (model.Type.ToLower())
                {
                    case "main":
                        var category = new Category
                        {
                            Name = model.Name,
                            Description = model.Description
                        };
                        _context.Categories.Add(category);
                        break;

                    case "sub":
                        if (!model.ParentId.HasValue)
                        {
                            return Json(new { success = false, message = "ParentId is required for subcategory" });
                        }

                        var subcategory = new Subcategory
                        {
                            Name = model.Name,
                            Description = model.Description,
                            CategoryID = model.ParentId.Value
                        };
                        _context.Subcategories.Add(subcategory);
                        break;

                    case "subsub":
                        if (!model.ParentId.HasValue)
                        {
                            return Json(new { success = false, message = "ParentId is required for sub-subcategory" });
                        }

                        var subSubcategory = new SubSubcategory
                        {
                            Name = model.Name,
                            Description = model.Description,
                            SubcategoryID = model.ParentId.Value
                        };
                        _context.SubSubcategories.Add(subSubcategory);
                        break;

                    default:
                        return Json(new { success = false, message = "Invalid category type" });
                }

                await _context.SaveChangesAsync();
                
                // Invalidate cache sau khi tạo
                InvalidateCache();
                
                return Json(new { success = true, message = "Danh mục đã được tạo thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin danh mục để chỉnh sửa
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategory(int id, string type)
        {
            try
            {
                object category = null;

                switch (type.ToLower())
                {
                    case "main":
                        category = await _context.Categories
                            .Where(c => c.CategoryID == id)
                            .Select(c => new
                            {
                                id = c.CategoryID,
                                name = c.Name,
                                description = c.Description,
                                type = "main"
                            })
                            .FirstOrDefaultAsync();
                        break;

                    case "sub":
                        category = await _context.Subcategories
                            .Where(sc => sc.SubcategoryID == id)
                            .Select(sc => new
                            {
                                id = sc.SubcategoryID,
                                name = sc.Name,
                                description = sc.Description,
                                parentId = sc.CategoryID,
                                type = "sub"
                            })
                            .FirstOrDefaultAsync();
                        break;

                    case "subsub":
                        category = await _context.SubSubcategories
                            .Where(ssc => ssc.SubSubcategoryID == id)
                            .Select(ssc => new
                            {
                                id = ssc.SubSubcategoryID,
                                name = ssc.Name,
                                description = ssc.Description,
                                parentId = ssc.SubcategoryID,
                                type = "subsub"
                            })
                            .FirstOrDefaultAsync();
                        break;

                    default:
                        return Json(new { success = false, message = "Invalid category type" });
                }

                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                return Json(new { success = true, data = category });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật danh mục
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateCategory([FromBody] CategoryUpdateModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                switch (model.Type.ToLower())
                {
                    case "main":
                        var category = await _context.Categories.FindAsync(model.Id);
                        if (category == null)
                        {
                            return Json(new { success = false, message = "Không tìm thấy danh mục" });
                        }

                        category.Name = model.Name;
                        category.Description = model.Description;
                        break;

                    case "sub":
                        var subcategory = await _context.Subcategories.FindAsync(model.Id);
                        if (subcategory == null)
                        {
                            return Json(new { success = false, message = "Không tìm thấy danh mục" });
                        }

                        subcategory.Name = model.Name;
                        subcategory.Description = model.Description;
                        break;

                    case "subsub":
                        var subSubcategory = await _context.SubSubcategories.FindAsync(model.Id);
                        if (subSubcategory == null)
                        {
                            return Json(new { success = false, message = "Không tìm thấy danh mục" });
                        }

                        subSubcategory.Name = model.Name;
                        subSubcategory.Description = model.Description;
                        break;

                    default:
                        return Json(new { success = false, message = "Invalid category type" });
                }

                await _context.SaveChangesAsync();
                
                // Invalidate cache sau khi tạo
                InvalidateCache();
                
                return Json(new { success = true, message = "Danh mục đã được cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa danh mục với cascade delete
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteCategory(int id, string type)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int deletedProducts = 0;
                int deletedSubCategories = 0;
                int deletedSubSubCategories = 0;

                switch (type.ToLower())
                {
                    case "main":
                        var category = await _context.Categories
                            .Include(c => c.Subcategories)
                                .ThenInclude(sc => sc.SubSubcategories)
                                    .ThenInclude(ssc => ssc.Products)
                            .FirstOrDefaultAsync(c => c.CategoryID == id);

                        if (category == null)
                        {
                            return Json(new { success = false, message = "Không tìm thấy danh mục" });
                        }

                        // Đếm các items sẽ bị xóa
                        deletedSubCategories = category.Subcategories.Count;
                        deletedSubSubCategories = category.Subcategories.SelectMany(sc => sc.SubSubcategories).Count();
                        deletedProducts = category.Subcategories
                            .SelectMany(sc => sc.SubSubcategories)
                            .SelectMany(ssc => ssc.Products)
                            .Count();

                        // Xóa CategoryAttributes trước
                        await DeleteCategoryRelatedData(id);

                        // Xóa cascade: Products -> SubSubcategories -> Subcategories -> Category
                        foreach (var subcategory in category.Subcategories)
                        {
                            foreach (var subsubcategory in subcategory.SubSubcategories)
                            {
                                // Xóa related data trước khi xóa products
                                await DeleteProductRelatedData(subsubcategory.Products.Select(p => p.ProductID).ToList());
                                _context.Products.RemoveRange(subsubcategory.Products);
                            }
                            _context.SubSubcategories.RemoveRange(subcategory.SubSubcategories);
                        }
                        _context.Subcategories.RemoveRange(category.Subcategories);
                        _context.Categories.Remove(category);
                        break;

                    case "sub":
                        var subCategoryToDelete = await _context.Subcategories
                            .Include(sc => sc.SubSubcategories)
                                .ThenInclude(ssc => ssc.Products)
                            .FirstOrDefaultAsync(sc => sc.SubcategoryID == id);

                        if (subCategoryToDelete == null)
                        {
                            return Json(new { success = false, message = "Không tìm thấy danh mục" });
                        }

                        // Đếm các items sẽ bị xóa
                        deletedSubSubCategories = subCategoryToDelete.SubSubcategories.Count;
                        deletedProducts = subCategoryToDelete.SubSubcategories.SelectMany(ssc => ssc.Products).Count();

                        // Xóa dữ liệu liên quan đến subcategory
                        await DeleteSubcategoryRelatedData(id);

                        // Xóa cascade: Products -> SubSubcategories -> Subcategory
                        foreach (var subsubcategory in subCategoryToDelete.SubSubcategories)
                        {
                            await DeleteProductRelatedData(subsubcategory.Products.Select(p => p.ProductID).ToList());
                            _context.Products.RemoveRange(subsubcategory.Products);
                        }
                        _context.SubSubcategories.RemoveRange(subCategoryToDelete.SubSubcategories);
                        _context.Subcategories.Remove(subCategoryToDelete);
                        break;

                    case "subsub":
                        var subSubcategory = await _context.SubSubcategories
                            .Include(ssc => ssc.Products)
                            .FirstOrDefaultAsync(ssc => ssc.SubSubcategoryID == id);

                        if (subSubcategory == null)
                        {
                            return Json(new { success = false, message = "Không tìm thấy danh mục" });
                        }

                        // Đếm các items sẽ bị xóa
                        deletedProducts = subSubcategory.Products.Count;

                        // Xóa dữ liệu liên quan đến subsubcategory
                        await DeleteSubSubcategoryRelatedData(id);

                        // Xóa cascade: Products -> SubSubcategory
                        _context.Products.RemoveRange(subSubcategory.Products);
                        _context.SubSubcategories.Remove(subSubcategory);
                        break;

                    default:
                        return Json(new { success = false, message = "Invalid category type" });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Invalidate cache sau khi xóa
                InvalidateCache();

                // Tạo thông báo chi tiết về những gì đã được xóa
                var messageItems = new List<string>();
                
                if (deletedProducts > 0) messageItems.Add($"{deletedProducts} sản phẩm");
                if (deletedSubSubCategories > 0) messageItems.Add($"{deletedSubSubCategories} danh mục con");
                if (deletedSubCategories > 0) messageItems.Add($"{deletedSubCategories} danh mục phụ");
                
                var message = $"Đã xóa thành công: {string.Join(", ", messageItems)}";
                message += "\n\nCác dữ liệu liên quan đã được xóa:\n";
                message += "• Hình ảnh sản phẩm\n";
                message += "• Giỏ hàng chứa sản phẩm\n";
                message += "• Danh sách yêu thích\n";
                message += "• Đánh giá sản phẩm\n";
                message += "• Đăng ký thông báo hết hàng\n";
                message += "• Lịch sử đơn hàng liên quan";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách parent categories cho dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetParentCategories(string type)
        {
            try
            {
                object categories = null;

                switch (type.ToLower())
                {
                    case "sub":
                        categories = await _context.Categories
                            .Select(c => new { value = c.CategoryID, text = c.Name })
                            .ToListAsync();
                        break;

                    case "subsub":
                        categories = await _context.Subcategories
                            .Include(sc => sc.Category)
                            .Select(sc => new 
                            { 
                                value = sc.SubcategoryID, 
                                text = $"{sc.Category.Name} > {sc.Name}" 
                            })
                            .ToListAsync();
                        break;

                    default:
                        return Json(new { success = false, message = "Invalid type" });
                }

                return Json(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Xóa cache khi có thay đổi data
        /// </summary>
        private void InvalidateCache()
        {
            _cache.Remove("categories_tree");
            _cache.Remove("category_statistics");
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Xóa dữ liệu liên quan đến products trước khi xóa products
        /// </summary>
        private async Task DeleteProductRelatedData(List<int> productIds)
        {
            if (!productIds.Any()) return;

            // Xóa ProductImages
            var productImages = await _context.ProductImages
                .Where(pi => productIds.Contains(pi.ProductID))
                .ToListAsync();
            _context.ProductImages.RemoveRange(productImages);

            // Xóa ProductAttributeValues
            var productAttributeValues = await _context.ProductAttributeValues
                .Where(pav => productIds.Contains(pav.ProductID))
                .ToListAsync();
            _context.ProductAttributeValues.RemoveRange(productAttributeValues);

            // Xóa ProductStockNotifications - THÊM MỚI
            // Khi xóa sản phẩm, cần xóa tất cả đăng ký thông báo hết hàng của users cho sản phẩm đó
            // Điều này đảm bảo không có orphaned records và không gửi thông báo cho sản phẩm đã bị xóa
            var productStockNotifications = await _context.ProductStockNotifications
                .Where(psn => productIds.Contains(psn.ProductID))
                .ToListAsync();
            _context.ProductStockNotifications.RemoveRange(productStockNotifications);

            // Xóa CartItems
            var cartItems = await _context.CartItems
                .Where(ci => productIds.Contains(ci.ProductID))
                .ToListAsync();
            _context.CartItems.RemoveRange(cartItems);

            // Xóa WishlistItems
            var wishlistItems = await _context.WishlistItems
                .Where(wi => productIds.Contains(wi.ProductID))
                .ToListAsync();
            _context.WishlistItems.RemoveRange(wishlistItems);

            // Xử lý OrderItems - Strategy: Set ProductID to NULL hoặc xóa hoàn toàn
            var allOrderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => productIds.Contains(oi.ProductID))
                .ToListAsync();

            // Phân loại OrderItems theo trạng thái Order
            var pendingOrderItems = allOrderItems
                .Where(oi => oi.Order.Status == "Pending" || oi.Order.Status == "Processing")
                .ToList();
            
            var completedOrderItems = allOrderItems
                .Where(oi => oi.Order.Status == "Delivered" || oi.Order.Status == "Shipped" || oi.Order.Status == "Completed")
                .ToList();

            // Xóa OrderItems của orders chưa hoàn thành
            _context.OrderItems.RemoveRange(pendingOrderItems);

            // Với orders đã hoàn thành: Có 2 option
            // Option 1: Xóa luôn (có thể gây mất dữ liệu lịch sử)
            _context.OrderItems.RemoveRange(completedOrderItems);

            // Option 2: Nếu muốn giữ lịch sử, có thể:
            // - Set ProductID = NULL (cần modify model)
            // - Tạo ProductName backup field
            // - Hoặc có archived_products table
            // foreach (var item in completedOrderItems)
            // {
            //     item.ProductID = null; // Cần modify model để cho phép nullable
            //     item.ProductName = item.Product.Name; // Backup tên sản phẩm
            // }

            // Xóa Reviews
            var reviews = await _context.Reviews
                .Where(r => productIds.Contains(r.ProductID))
                .ToListAsync();
            _context.Reviews.RemoveRange(reviews);

            // Xóa VoucherProducts
            var voucherProducts = await _context.VoucherProducts
                .Where(vp => productIds.Contains(vp.ProductID))
                .ToListAsync();
            _context.VoucherProducts.RemoveRange(voucherProducts);
        }

        /// <summary>
        /// Xóa dữ liệu liên quan đến category trước khi xóa category
        /// </summary>
        private async Task DeleteCategoryRelatedData(int categoryId)
        {
            // Xóa CategoryAttributes liên quan đến category
            var categoryAttributes = await _context.CategoryAttributes
                .Where(ca => ca.CategoryID == categoryId)
                .ToListAsync();
            _context.CategoryAttributes.RemoveRange(categoryAttributes);
        }

        /// <summary>
        /// Xóa dữ liệu liên quan đến subcategory trước khi xóa subcategory
        /// </summary>
        private async Task DeleteSubcategoryRelatedData(int subcategoryId)
        {
            // Thường subcategory không có relationship trực tiếp nào khác
            // Nếu có thêm bảng nào reference đến Subcategory thì thêm vào đây
        }

        /// <summary>
        /// Xóa dữ liệu liên quan đến subsubcategory trước khi xóa subsubcategory  
        /// </summary>
        private async Task DeleteSubSubcategoryRelatedData(int subSubcategoryId)
        {
            // Lấy tất cả product IDs trong subsubcategory này
            var productIds = await _context.Products
                .Where(p => p.SubSubcategoryID == subSubcategoryId)
                .Select(p => p.ProductID)
                .ToListAsync();

            // Xóa tất cả related data của products
            await DeleteProductRelatedData(productIds);
        }

        #endregion
    }

    #region DTOs/Models

    public class CategoryCreateModel
    {
        public string Type { get; set; } // "main", "sub", "subsub"
        public string Name { get; set; }
        public string Description { get; set; }
        public int? ParentId { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public bool Active { get; set; } = true;
    }

    public class CategoryUpdateModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public bool Active { get; set; }
    }

    #endregion
}
