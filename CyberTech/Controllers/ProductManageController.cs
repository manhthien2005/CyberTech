using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using CyberTech.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;

namespace CyberTechShop.Controllers
{
    [Authorize (Roles = "Admin,Staff,Support,Manager,SuperAdmin")]
    public class ProductManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public ProductManageController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetCategories()
        {
            var categories = _context.Categories
                .Include(c => c.Subcategories)
                    .ThenInclude(sc => sc.SubSubcategories)
                .Select(c => new
                {
                    categoryID = c.CategoryID,
                    name = c.Name,
                    subcategories = c.Subcategories.Select(sc => new
                    {
                        subcategoryID = sc.SubcategoryID,
                        name = sc.Name,
                        subSubcategories = sc.SubSubcategories.Select(ssc => new
                        {
                            subSubcategoryID = ssc.SubSubcategoryID,
                            name = ssc.Name
                        }).ToList()
                    }).ToList()
                }).ToList();
            return Json(categories);
        }

        [HttpGet]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client)] // Cache 30s
        public async Task<IActionResult> GetProducts(string search, string status, string sort, int page = 1, int subsubcategoryId = 0)
        {
            var query = _context.Products.AsNoTracking() // Không tracking để tăng tốc
                .Where(p => subsubcategoryId == 0 || p.SubSubcategoryID == subsubcategoryId);

            // Apply filters
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search));

            if (status == "1")
                query = query.Where(p => p.Stock > 0);
            else if (status == "0")
                query = query.Where(p => p.Stock == 0);

            // Apply sorting
            switch (sort)
            {
                case "name_asc": query = query.OrderBy(p => p.Name); break;
                case "name_desc": query = query.OrderByDescending(p => p.Name); break;
                case "price_asc": query = query.OrderBy(p => p.Price); break;
                case "price_desc": query = query.OrderByDescending(p => p.Price); break;
                default: query = query.OrderBy(p => p.Name); break;
            }

            var total = await query.CountAsync();
            var pageSize = 16;

            // Optimized query với minimal joins
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.ProductID,
                    name = p.Name,
                    description = p.Description ?? "",
                    price = p.SalePrice.HasValue ? p.SalePrice.Value :
                           (p.SalePercentage.HasValue ? p.Price * (1 - (p.SalePercentage.Value / 100)) : p.Price),
                    originalPrice = p.Price,
                    salePercentage = p.SalePercentage,
                    salePrice = p.SalePrice,
                    isOnSale = (p.SalePrice.HasValue && p.SalePrice.Value < p.Price) || p.SalePercentage.HasValue,
                    stock = p.Stock,
                    brand = p.Brand ?? "",
                    subSubcategoryId = p.SubSubcategoryID,
                    imageUrl = p.ProductImages.Where(pi => pi.IsPrimary).Select(pi => pi.ImageURL).FirstOrDefault() ?? ""
                })
                .ToListAsync();

            // Sử dụng cache cho category names
            var subSubcategoryIds = products.Select(p => p.subSubcategoryId).Distinct().ToList();
            var categoryDict = new Dictionary<int, string>();

            foreach (var id in subSubcategoryIds)
            {
                var cacheKey = $"category_name_{id}";
                if (!_cache.TryGetValue(cacheKey, out string categoryName))
                {
                    var categoryData = await _context.SubSubcategories.AsNoTracking()
                        .Where(ssc => ssc.SubSubcategoryID == id)
                        .Select(ssc => ssc.Subcategory.Category.Name + " > " +
                                     ssc.Subcategory.Name + " > " +
                                     ssc.Name)
                        .FirstOrDefaultAsync();

                    categoryName = categoryData ?? "";
                    _cache.Set(cacheKey, categoryName, TimeSpan.FromMinutes(30)); // Cache 30 phút
                }
                categoryDict[id] = categoryName;
            }

            // Map kết quả cuối
            var result = products.Select(p => new
            {
                p.id,
                p.name,
                p.description,
                p.price,
                p.originalPrice,
                p.salePercentage,
                p.salePrice,
                p.isOnSale,
                p.stock,
                p.brand,
                categoryName = categoryDict.GetValueOrDefault(p.subSubcategoryId, ""),
                p.imageUrl,
                attributes = new object[0] // Bỏ attributes để tăng tốc
            }).ToList();

            return Json(new { products = result, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
        }

        [HttpPost]
        public IActionResult AddCategory(string name)
        {
            var category = new Category { Name = name };
            _context.Categories.Add(category);
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult AddSubcategory(string name, int parentId)
        {
            var subcategory = new Subcategory { Name = name, CategoryID = parentId };
            _context.Subcategories.Add(subcategory);
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult AddSubSubcategory(string name, int parentId)
        {
            var subsubcategory = new SubSubcategory { Name = name, SubcategoryID = parentId };
            _context.SubSubcategories.Add(subsubcategory);
            _context.SaveChanges();
            return Ok();
        }

        [HttpDelete]
        [Route("DeleteCategory/{id}")]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories
                .Include(c => c.Subcategories)
                    .ThenInclude(sc => sc.SubSubcategories)
                .FirstOrDefault(c => c.CategoryID == id);
            if (category == null) return NotFound();
            _context.Categories.Remove(category);
            _context.SaveChanges();
            return Ok();
        }

        [HttpDelete]
        [Route("DeleteSubcategory/{id}")]
        public IActionResult DeleteSubcategory(int id)
        {
            var subcategory = _context.Subcategories
                .Include(sc => sc.SubSubcategories)
                .FirstOrDefault(sc => sc.SubcategoryID == id);
            if (subcategory == null) return NotFound();
            _context.Subcategories.Remove(subcategory);
            _context.SaveChanges();
            return Ok();
        }

        [HttpDelete]
        [Route("DeleteSubSubcategory/{id}")]
        public IActionResult DeleteSubSubcategory(int id)
        {
            var subsubcategory = _context.SubSubcategories.FirstOrDefault(ssc => ssc.SubSubcategoryID == id);
            if (subsubcategory == null) return NotFound();
            _context.SubSubcategories.Remove(subsubcategory);
            _context.SaveChanges();
            return Ok();
        }

        [HttpPut]
        public IActionResult UpdateCategory(int id, string name)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            category.Name = name;
            _context.SaveChanges();
            return Ok();
        }

        [HttpPut]
        public IActionResult UpdateSubcategory(int id, string name)
        {
            var subcategory = _context.Subcategories.Find(id);
            if (subcategory == null) return NotFound();
            subcategory.Name = name;
            _context.SaveChanges();
            return Ok();
        }

        [HttpPut]
        public IActionResult UpdateSubSubcategory(int id, string name)
        {
            var subsubcategory = _context.SubSubcategories.Find(id);
            if (subsubcategory == null) return NotFound();
            subsubcategory.Name = name;
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromForm] Product product, [FromForm] List<IFormFile> images, [FromForm] int? PrimaryImageIndex, [FromForm] string Attributes)
        {
            try
            {
                // Log thông tin nhận được
                Console.WriteLine($"Received product: Name={product.Name}, Price={product.Price}, Stock={product.Stock}, SubSubcategoryID={product.SubSubcategoryID}");
                Console.WriteLine($"Images count: {images?.Count ?? 0}");
                Console.WriteLine($"Primary image index: {PrimaryImageIndex}");
                Console.WriteLine($"Attributes: {Attributes}");

                // Validate required fields
                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    return BadRequest("Tên sản phẩm không được để trống");
                }

                if (product.Price <= 0)
                {
                    return BadRequest("Giá sản phẩm phải lớn hơn 0");
                }

                if (product.SubSubcategoryID <= 0)
                {
                    return BadRequest("Vui lòng chọn danh mục chi tiết");
                }

                // Validate SubSubcategoryID exists
                var subSubcategory = await _context.SubSubcategories
                    .Include(ssc => ssc.Subcategory)
                        .ThenInclude(sc => sc.Category)
                    .FirstOrDefaultAsync(ssc => ssc.SubSubcategoryID == product.SubSubcategoryID);

                if (subSubcategory == null)
                {
                    return BadRequest("Danh mục chi tiết không tồn tại");
                }

                // Validate images
                if (images == null || !images.Any() || images.All(i => i.Length == 0))
                {
                    return BadRequest("Vui lòng chọn ít nhất một ảnh sản phẩm");
                }

                // Set default values
                if (string.IsNullOrWhiteSpace(product.Description))
                    product.Description = "";

                if (string.IsNullOrWhiteSpace(product.Brand))
                    product.Brand = "";

                // Set timestamps
                //product.CreatedAt = DateTime.Now;
                //product.UpdatedAt = DateTime.Now;

                // Add product
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Product added with ID: {product.ProductID}");

                // Handle images
                var productImages = new List<ProductImage>();
                var validImageIndex = 0;

                for (int i = 0; i < images.Count; i++)
                {
                    var image = images[i];
                    if (image.Length > 0)
                    {
                        // Validate file type
                        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                        if (!allowedTypes.Contains(image.ContentType.ToLower()))
                        {
                            Console.WriteLine($"Skipping invalid image type: {image.ContentType}");
                            continue;
                        }

                        // Validate file size (5MB max)
                        if (image.Length > 5 * 1024 * 1024)
                        {
                            return BadRequest($"Ảnh {image.FileName} vượt quá 5MB");
                        }

                        // Generate unique filename
                        var fileExtension = Path.GetExtension(image.FileName);
                        var fileName = $"{Guid.NewGuid()}{fileExtension}";
                        var uploadDir = Path.Combine("wwwroot", "uploads", "products");
                        var filePath = Path.Combine(uploadDir, fileName);

                        // Ensure directory exists
                        Directory.CreateDirectory(uploadDir);

                        // Save file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        // Determine if this is primary image
                        bool isPrimary = (PrimaryImageIndex.HasValue && PrimaryImageIndex.Value == i) ||
                                        (productImages.Count == 0); // First valid image as fallback

                        // Add to database
                        productImages.Add(new ProductImage
                        {
                            ProductID = product.ProductID,
                            ImageURL = $"/uploads/products/{fileName}",
                            IsPrimary = isPrimary,
                            DisplayOrder = validImageIndex,
                            //CreatedAt = DateTime.Now
                        });

                        Console.WriteLine($"Added image: {fileName}, IsPrimary: {isPrimary}");
                        validImageIndex++;
                    }
                }

                if (productImages.Any())
                {
                    // Ensure at least one image is primary
                    if (!productImages.Any(pi => pi.IsPrimary))
                    {
                        productImages[0].IsPrimary = true;
                    }

                    _context.ProductImages.AddRange(productImages);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Saved {productImages.Count} images to database");
                }

                // Handle attributes if provided
                if (!string.IsNullOrWhiteSpace(Attributes))
                {
                    try
                    {
                        var attributesList = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(Attributes);
                        // Process attributes if needed - for now just log
                        Console.WriteLine($"Received {attributesList?.Count ?? 0} attributes");
                    }
                    catch (Exception attrEx)
                    {
                        Console.WriteLine($"Error processing attributes: {attrEx.Message}");
                        // Don't fail the entire operation for attribute errors
                    }
                }

                return Ok(new
                {
                    success = true,
                    productId = product.ProductID,
                    message = "Sản phẩm đã được thêm thành công",
                    imagesCount = productImages.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding product: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSubcategories(int categoryId)
        {
            var subcategories = await _context.Subcategories
                .Where(sc => sc.CategoryID == categoryId)
                .Select(sc => new { id = sc.SubcategoryID, name = sc.Name })
                .ToListAsync();
            return Json(subcategories);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubSubcategories(int subcategoryId)
        {
            var subSubcategories = await _context.SubSubcategories
                .Where(ssc => ssc.SubcategoryID == subcategoryId)
                .Select(ssc => new { id = ssc.SubSubcategoryID, name = ssc.Name })
                .ToListAsync();
            return Json(subSubcategories);
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductAttributeValues)
                        .ThenInclude(pav => pav.AttributeValue)
                            .ThenInclude(av => av.ProductAttribute)
                    .Include(p => p.SubSubcategory)
                        .ThenInclude(ssc => ssc.Subcategory)
                            .ThenInclude(sc => sc.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                    return NotFound("Sản phẩm không tồn tại");

                var result = new
                {
                    id = product.ProductID,
                    name = product.Name,
                    description = product.Description ?? "",
                    price = product.Price,
                    salePrice = product.SalePrice,
                    salePercentage = product.SalePercentage,
                    effectivePrice = product.GetEffectivePrice(),
                    isOnSale = product.IsOnSale,
                    stock = product.Stock,
                    brand = product.Brand ?? "",
                    isActive = product.Stock > 0,
                    categoryId = product.SubSubcategory.Subcategory.CategoryID,
                    subcategoryId = product.SubSubcategory.SubcategoryID,
                    subSubcategoryId = product.SubSubcategoryID,
                    categoryName = product.SubSubcategory.Subcategory.Category.Name + " > " +
                                  product.SubSubcategory.Subcategory.Name + " > " +
                                  product.SubSubcategory.Name,
                    images = product.ProductImages.Select(pi => new
                    {
                        id = pi.ImageID,
                        url = pi.ImageURL,
                        isPrimary = pi.IsPrimary,
                        fileName = Path.GetFileName(pi.ImageURL)
                    }).ToList(),
                    attributes = product.ProductAttributeValues.Select(pav => new
                    {
                        name = pav.AttributeValue.ProductAttribute.AttributeName,
                        value = pav.AttributeValue.ValueName
                    }).ToList()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting product: {ex.Message}");
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProduct([FromForm] Product product, [FromForm] List<IFormFile> images, [FromForm] int? PrimaryImageIndex, [FromForm] string Attributes, [FromForm] string DeletedImageIds, [FromForm] decimal? SalePrice)
        {
            try
            {
                Console.WriteLine($"Updating product ID: {product.ProductID}");
                Console.WriteLine($"Product details: Name={product.Name}, Price={product.Price}, Stock={product.Stock}");
                Console.WriteLine($"Sale Price: {SalePrice}");

                // Validate required fields
                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    return BadRequest("Tên sản phẩm không được để trống");
                }

                if (product.Price <= 0)
                {
                    return BadRequest("Giá sản phẩm phải lớn hơn 0");
                }

                if (product.SubSubcategoryID <= 0)
                {
                    return BadRequest("Vui lòng chọn danh mục chi tiết");
                }

                // Find existing product
                var existingProduct = await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductAttributeValues)
                    .FirstOrDefaultAsync(p => p.ProductID == product.ProductID);

                if (existingProduct == null)
                {
                    return NotFound("Sản phẩm không tồn tại");
                }

                // Validate SubSubcategoryID exists
                var subSubcategory = await _context.SubSubcategories
                    .Include(ssc => ssc.Subcategory)
                        .ThenInclude(sc => sc.Category)
                    .FirstOrDefaultAsync(ssc => ssc.SubSubcategoryID == product.SubSubcategoryID);

                if (subSubcategory == null)
                {
                    return BadRequest("Danh mục chi tiết không tồn tại");
                }

                // Update product fields
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description ?? "";
                existingProduct.Price = product.Price;
                existingProduct.Stock = product.Stock;
                existingProduct.Brand = product.Brand ?? "";
                existingProduct.SubSubcategoryID = product.SubSubcategoryID;
                
                // Handle sale price
                if (SalePrice.HasValue && SalePrice.Value > 0 && SalePrice.Value < product.Price)
                {
                    existingProduct.SalePrice = SalePrice.Value;
                    existingProduct.SalePercentage = null; // Clear percentage if using fixed price
                    Console.WriteLine($"Set sale price to: {SalePrice.Value}");
                }
                else
                {
                    existingProduct.SalePrice = null;
                    existingProduct.SalePercentage = null;
                    Console.WriteLine("Cleared sale price");
                }
                
                //existingProduct.UpdatedAt = DateTime.Now;

                // Handle deleted images
                if (!string.IsNullOrWhiteSpace(DeletedImageIds))
                {
                    var deletedIds = DeletedImageIds.Split(',').Select(int.Parse).ToList();
                    var imagesToDelete = existingProduct.ProductImages.Where(pi => deletedIds.Contains(pi.ImageID)).ToList();

                    foreach (var imageToDelete in imagesToDelete)
                    {
                        // Delete physical file
                        var filePath = Path.Combine("wwwroot", imageToDelete.ImageURL.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }

                        _context.ProductImages.Remove(imageToDelete);
                    }
                }

                // Handle new images
                if (images != null && images.Any(i => i.Length > 0))
                {
                    var currentImageCount = existingProduct.ProductImages.Count;

                    for (int i = 0; i < images.Count; i++)
                    {
                        var image = images[i];
                        if (image.Length > 0)
                        {
                            // Validate file type
                            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                            if (!allowedTypes.Contains(image.ContentType.ToLower()))
                            {
                                continue;
                            }

                            // Validate file size (5MB max)
                            if (image.Length > 5 * 1024 * 1024)
                            {
                                return BadRequest($"Ảnh {image.FileName} vượt quá 5MB");
                            }

                            // Generate unique filename
                            var fileExtension = Path.GetExtension(image.FileName);
                            var fileName = $"{Guid.NewGuid()}{fileExtension}";
                            var uploadDir = Path.Combine("wwwroot", "uploads", "products");
                            var filePath = Path.Combine(uploadDir, fileName);

                            // Ensure directory exists
                            Directory.CreateDirectory(uploadDir);

                            // Save file
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            // Add to database
                            var newImage = new ProductImage
                            {
                                ProductID = existingProduct.ProductID,
                                ImageURL = $"/uploads/products/{fileName}",
                                IsPrimary = false, // Will be set later based on user selection
                                DisplayOrder = currentImageCount,
                                //CreatedAt = DateTime.Now
                            };

                            existingProduct.ProductImages.Add(newImage);
                            currentImageCount++;
                        }
                    }
                }

                // Update primary image
                if (PrimaryImageIndex.HasValue)
                {
                    var allImages = existingProduct.ProductImages.ToList();
                    for (int i = 0; i < allImages.Count; i++)
                    {
                        allImages[i].IsPrimary = (i == PrimaryImageIndex.Value);
                    }
                }
                else if (!existingProduct.ProductImages.Any(pi => pi.IsPrimary) && existingProduct.ProductImages.Any())
                {
                    // Ensure at least one image is primary
                    existingProduct.ProductImages.First().IsPrimary = true;
                }

                // Clear existing attributes and add new ones
                if (existingProduct.ProductAttributeValues.Any())
                {
                    _context.ProductAttributeValues.RemoveRange(existingProduct.ProductAttributeValues);
                }

                // Handle attributes if provided
                if (!string.IsNullOrWhiteSpace(Attributes))
                {
                    try
                    {
                        var attributesList = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(Attributes);
                        Console.WriteLine($"Processing {attributesList?.Count ?? 0} attributes for update");
                        
                        if (attributesList != null && attributesList.Any())
                        {
                            foreach (var attr in attributesList)
                            {
                                if (attr.ContainsKey("name") && attr.ContainsKey("value") && 
                                    !string.IsNullOrWhiteSpace(attr["name"]) && !string.IsNullOrWhiteSpace(attr["value"]))
                                {
                                    var attributeName = attr["name"].Trim();
                                    var attributeValue = attr["value"].Trim();
                                    
                                    // Find or create ProductAttribute
                                    var productAttribute = await _context.ProductAttributes
                                        .FirstOrDefaultAsync(pa => pa.AttributeName == attributeName);
                                    
                                    if (productAttribute == null)
                                    {
                                        productAttribute = new ProductAttribute
                                        {
                                            AttributeName = attributeName
                                        };
                                        _context.ProductAttributes.Add(productAttribute);
                                        await _context.SaveChangesAsync(); // Save to get ID
                                    }
                                    
                                    // Find or create AttributeValue
                                    var attributeValueEntity = await _context.AttributeValues
                                        .FirstOrDefaultAsync(av => av.AttributeID == productAttribute.AttributeID && 
                                                           av.ValueName == attributeValue);
                                    
                                    if (attributeValueEntity == null)
                                    {
                                        attributeValueEntity = new AttributeValue
                                        {
                                            AttributeID = productAttribute.AttributeID,
                                            ValueName = attributeValue
                                        };
                                        _context.AttributeValues.Add(attributeValueEntity);
                                        await _context.SaveChangesAsync(); // Save to get ID
                                    }
                                    
                                    // Create ProductAttributeValue
                                    var productAttributeValue = new ProductAttributeValue
                                    {
                                        ProductID = existingProduct.ProductID,
                                        ValueID = attributeValueEntity.ValueID
                                    };
                                    
                                    existingProduct.ProductAttributeValues.Add(productAttributeValue);
                                    Console.WriteLine($"Added attribute: {attributeName} = {attributeValue}");
                                }
                            }
                        }
                    }
                    catch (Exception attrEx)
                    {
                        Console.WriteLine($"Error processing attributes: {attrEx.Message}");
                        Console.WriteLine($"Attributes JSON: {Attributes}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    productId = existingProduct.ProductID,
                    message = "Sản phẩm đã được cập nhật thành công",
                    imagesCount = existingProduct.ProductImages.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
    }
}