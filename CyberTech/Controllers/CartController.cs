using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Services;
using CyberTech.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CyberTech.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<CartController> _logger;
        private readonly ICartService _cartService;
        private readonly IEmailService _emailService;

        public CartController(
            ApplicationDbContext context,
            IUserService userService,
            ILogger<CartController> logger,
            ICartService cartService,
            IEmailService emailService)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
            _cartService = cartService;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = true, count = 0 });
                }

                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                int count = await _cartService.GetCartItemCountAsync(user.UserID);
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy số lượng giỏ hàng" });
            }
        }

        private decimal CalculateCartTotals(Cart cart, Voucher voucher, out decimal discountAmount)
        {
            // Calculate total price using original prices
            decimal totalPrice = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);

            // Calculate product discount (from sales)
            decimal productDiscountAmount = 0;
            foreach (var item in cart.CartItems)
            {
                var product = item.Product;
                var originalPrice = product.Price * item.Quantity;
                var effectivePrice = product.GetEffectivePrice() * item.Quantity;
                productDiscountAmount += originalPrice - effectivePrice;
            }

            // Calculate amount after product discount
            decimal amountAfterProductDiscount = totalPrice - productDiscountAmount;

            // Get user's rank discount
            var user = _context.Users
                .Include(u => u.Rank)
                .FirstOrDefault(u => u.UserID == cart.UserID);

            // Calculate rank discount on amount after product discount
            decimal rankDiscountPercent = user?.Rank?.DiscountPercent ?? 0;
            decimal rankDiscountAmount = amountAfterProductDiscount * (rankDiscountPercent / 100);

            // Calculate voucher discount
            decimal voucherDiscountAmount = 0;
            if (voucher != null && IsVoucherValid(voucher, cart))
            {
                // Apply voucher discount on the amount after product and rank discounts
                decimal amountAfterRankDiscount = amountAfterProductDiscount - rankDiscountAmount;

                if (voucher.AppliesTo == "Order")
                {
                    // Apply to entire order
                    if (voucher.DiscountType == "PERCENT")
                    {
                        voucherDiscountAmount = amountAfterRankDiscount * (voucher.DiscountValue / 100);
                    }
                    else if (voucher.DiscountType == "FIXED")
                    {
                        voucherDiscountAmount = Math.Min(voucher.DiscountValue, amountAfterRankDiscount);
                    }
                }
                else if (voucher.AppliesTo == "Product")
                {
                    // Apply to specific products only
                    var applicableProductIds = voucher.VoucherProducts.Select(vp => vp.ProductID).ToList();
                    var eligibleCartItems = cart.CartItems.Where(ci => applicableProductIds.Contains(ci.ProductID)).ToList();

                    if (eligibleCartItems.Any())
                    {
                        // Calculate eligible total after product discount
                        decimal eligibleTotal = 0;
                        foreach (var item in eligibleCartItems)
                        {
                            decimal originalItemPrice = item.Product.Price * item.Quantity;
                            decimal itemProductDiscount = originalItemPrice - (item.Product.GetEffectivePrice() * item.Quantity);
                            decimal itemAfterProductDiscount = originalItemPrice - itemProductDiscount;
                            decimal itemRankDiscount = itemAfterProductDiscount * (rankDiscountPercent / 100);
                            decimal itemAfterAllDiscounts = itemAfterProductDiscount - itemRankDiscount;
                            eligibleTotal += itemAfterAllDiscounts;
                        }

                        if (voucher.DiscountType == "PERCENT")
                        {
                            voucherDiscountAmount = eligibleTotal * (voucher.DiscountValue / 100);
                        }
                        else if (voucher.DiscountType == "FIXED")
                        {
                            voucherDiscountAmount = Math.Min(voucher.DiscountValue, eligibleTotal);
                        }
                    }
                }

                // Ensure voucher discount doesn't exceed the remaining amount
                voucherDiscountAmount = Math.Min(voucherDiscountAmount, amountAfterRankDiscount);
            }

            // Total discount is sum of all discounts
            discountAmount = productDiscountAmount + rankDiscountAmount + voucherDiscountAmount;

            // Final price is total price minus all discounts
            decimal finalPrice = totalPrice - discountAmount;

            // Update cart's TotalPrice to original price (not discounted)
            cart.TotalPrice = totalPrice;

            return finalPrice;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index(string voucherCode = null)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null) return NotFound();

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    cart = new Cart { UserID = user.UserID, TotalPrice = 0 };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var userAddresses = await _userService.GetUserAddressesAsync(user.UserID);

                // Check if we need to apply a voucher from the parameter
                if (!string.IsNullOrEmpty(voucherCode))
                {
                    // Find user's voucher by code
                    var userVoucher = await _context.UserVouchers
                        .Include(uv => uv.Voucher)
                        .FirstOrDefaultAsync(uv =>
                            uv.Voucher.Code == voucherCode &&
                            uv.UserID == user.UserID &&
                            !uv.IsUsed &&
                            uv.Voucher.IsActive &&
                            uv.Voucher.ValidTo > DateTime.Now);

                    if (userVoucher != null)
                    {
                        // Store the voucher ID in session
                        HttpContext.Session.SetInt32("AppliedVoucherId", userVoucher.VoucherID);
                        HttpContext.Session.SetInt32("AppliedUserVoucherId", userVoucher.UserVoucherID);

                        // Add success message
                        TempData["SuccessMessage"] = $"Đã áp dụng mã giảm giá {voucherCode}";
                    }
                }

                var appliedVoucherId = HttpContext.Session.GetInt32("AppliedVoucherId");
                Voucher appliedVoucher = null;
                if (appliedVoucherId.HasValue)
                {
                    appliedVoucher = await _context.Vouchers
                        .Include(v => v.VoucherProducts)
                        .FirstOrDefaultAsync(v => v.VoucherID == appliedVoucherId.Value);
                }

                // Calculate all discounts and final price
                decimal totalDiscountAmount;
                decimal finalPrice = CalculateCartTotals(cart, appliedVoucher, out totalDiscountAmount);

                // Calculate individual discount amounts
                decimal productDiscountAmount = 0;
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;
                    var originalPrice = product.Price * item.Quantity;
                    var effectivePrice = product.GetEffectivePrice() * item.Quantity;
                    productDiscountAmount += originalPrice - effectivePrice;
                }

                decimal amountAfterProductDiscount = cart.TotalPrice - productDiscountAmount;
                decimal rankDiscountPercent = user.Rank?.DiscountPercent ?? 0;
                decimal rankDiscountAmount = amountAfterProductDiscount * (rankDiscountPercent / 100);
                decimal voucherDiscountAmount = totalDiscountAmount - productDiscountAmount - rankDiscountAmount;

                await _context.SaveChangesAsync();

                var model = new CartViewModel
                {
                    Cart = cart,
                    CartItems = cart.CartItems.ToList(),
                    UserAddresses = userAddresses,
                    AppliedVoucher = appliedVoucher,
                    RankDiscountPercent = rankDiscountPercent,
                    RankDiscountAmount = rankDiscountAmount,
                    RankName = user.Rank?.RankName ?? "Thành viên",
                    Subtotal = cart.TotalPrice,  // Original price total
                    ProductDiscountAmount = productDiscountAmount,
                    VoucherDiscountAmount = voucherDiscountAmount,
                    TotalDiscount = totalDiscountAmount,
                    FinalTotal = finalPrice
                };

                // Add rank info to ViewBag for the view
                ViewBag.UserRank = new
                {
                    Name = user.Rank?.RankName ?? "Thành viên",
                    DiscountPercent = rankDiscountPercent
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart for user {Email}", User.Identity.Name);
                return View(new CartViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("User attempted to add to cart without being authenticated");
                    return Json(new
                    {
                        success = false,
                        message = "Bạn cần đăng nhập tài khoản để thêm sản phẩm vào giỏ hàng",
                        requireLogin = true
                    });
                }
                _logger.LogDebug("User is authenticated");
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null || product.Stock < quantity)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc không đủ hàng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    cart = new Cart { UserID = user.UserID, TotalPrice = 0 };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductID == productId);
                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                    if (cartItem.Quantity > product.Stock)
                    {
                        cartItem.Quantity = product.Stock;
                    }
                    cartItem.Subtotal = cartItem.Quantity * product.GetEffectivePrice();
                }
                else
                {
                    cartItem = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = productId,
                        Quantity = quantity,
                        Subtotal = quantity * product.GetEffectivePrice()
                    };
                    cart.CartItems.Add(cartItem);
                }

                cart.TotalPrice = cart.CartItems.Sum(ci => ci.Subtotal);
                await _context.SaveChangesAsync();

                // Get total cart item count for badge
                int cartCount = await _cartService.GetCartItemCountAsync(user.UserID);

                return Json(new
                {
                    success = true,
                    message = "Đã thêm vào giỏ hàng",
                    cart = new
                    {
                        totalPrice = cart.TotalPrice,
                        cartItemsCount = cart.CartItems.Count
                    },
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart for product {ProductId}", productId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm vào giỏ hàng" });
            }
        }

        // Helper method to calculate the effective price based on sale options
        private decimal CalculateEffectivePrice(Product product)
        {
            var effectivePrice = product.GetEffectivePrice();
            return effectivePrice;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng" });
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                cart.CartItems.Remove(cartItem);
                cart.TotalPrice = cart.CartItems.Sum(ci => ci.Subtotal);
                await _context.SaveChangesAsync();

                // Get total cart item count for badge
                int cartCount = await _cartService.GetCartItemCountAsync(user.UserID);

                return Json(new
                {
                    success = true,
                    message = "Đã xóa sản phẩm khỏi giỏ hàng",
                    cart = new
                    {
                        totalPrice = cart.TotalPrice,
                        cartItemsCount = cart.CartItems.Count
                    },
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = true, message = "Giỏ hàng đã trống", cartCount = 0 });
                }

                _context.CartItems.RemoveRange(cart.CartItems);
                cart.TotalPrice = 0;
                HttpContext.Session.Remove("AppliedVoucherId");
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã xóa toàn bộ giỏ hàng",
                    cartCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {Email}", User.Identity.Name);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa giỏ hàng" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng" });
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });
                }

                if (quantity >= cartItem.Product.Stock)
                {
                    quantity = cartItem.Product.Stock;
                }

                cartItem.Quantity = quantity;
                cartItem.Subtotal = cartItem.Product.Price * quantity;  // Use original price

                var appliedVoucherId = HttpContext.Session.GetInt32("AppliedVoucherId");
                Voucher appliedVoucher = null;
                if (appliedVoucherId.HasValue)
                {
                    appliedVoucher = await _context.Vouchers
                        .Include(v => v.VoucherProducts)
                        .FirstOrDefaultAsync(v => v.VoucherID == appliedVoucherId.Value);
                }

                // Calculate all discounts and final price
                decimal totalDiscountAmount;
                decimal finalPrice = CalculateCartTotals(cart, appliedVoucher, out totalDiscountAmount);

                // Calculate individual discount amounts
                decimal productDiscountAmount = 0;
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;
                    var originalPrice = product.Price * item.Quantity;
                    var effectivePrice = product.GetEffectivePrice() * item.Quantity;
                    productDiscountAmount += originalPrice - effectivePrice;
                }

                decimal amountAfterProductDiscount = cart.TotalPrice - productDiscountAmount;
                decimal rankDiscountPercent = user.Rank?.DiscountPercent ?? 0;
                decimal rankDiscountAmount = amountAfterProductDiscount * (rankDiscountPercent / 100);
                decimal voucherDiscountAmount = totalDiscountAmount - productDiscountAmount - rankDiscountAmount;

                await _context.SaveChangesAsync();

                // Get total cart item count for badge
                int cartCount = await _cartService.GetCartItemCountAsync(user.UserID);

                return Json(new
                {
                    success = true,
                    message = "Đã cập nhật số lượng",
                    subtotal = cartItem.Subtotal,
                    cart = new
                    {
                        totalPrice = cart.TotalPrice,  // Original price total
                        productDiscountAmount = productDiscountAmount,
                        rankDiscountAmount = rankDiscountAmount,
                        voucherDiscountAmount = voucherDiscountAmount,
                        totalDiscountAmount = totalDiscountAmount,
                        finalPrice = finalPrice,
                        cartItemsCount = cart.CartItems.Count
                    },
                    discountType = appliedVoucher?.DiscountType ?? "FIXED",
                    discountPercent = appliedVoucher?.DiscountType == "PERCENT" ? appliedVoucher.DiscountValue : 0,
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for cart item {CartItemId}", cartItemId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật số lượng" });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ApplyVoucher(string voucherCode)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                // First check if the voucher exists at all
                var voucher = await _context.Vouchers
                    .Include(v => v.VoucherProducts)
                    .FirstOrDefaultAsync(v => v.Code == voucherCode && v.IsActive && v.ValidTo > DateTime.Now);

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn" });
                }

                // Check if the voucher has available quantity
                if (voucher.QuantityAvailable.HasValue && voucher.QuantityAvailable <= 0)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng" });
                }

                // Find the voucher in user's voucher collection if it's not system-wide
                // or check if user has already claimed this system-wide voucher
                UserVoucher userVoucher = null;

                if (voucher.IsSystemWide)
                {
                    // For system-wide vouchers, check if the user has already claimed it
                    userVoucher = await _context.UserVouchers
                        .FirstOrDefaultAsync(uv => uv.VoucherID == voucher.VoucherID && uv.UserID == user.UserID && !uv.IsUsed);

                    // If user hasn't claimed this system-wide voucher yet, create a new UserVoucher entry
                    if (userVoucher == null)
                    {
                        userVoucher = new UserVoucher
                        {
                            UserID = user.UserID,
                            VoucherID = voucher.VoucherID,
                            AssignedDate = DateTime.Now,
                            IsUsed = false
                        };

                        _context.UserVouchers.Add(userVoucher);
                        await _context.SaveChangesAsync();

                        // Decrease quantity available if applicable
                        if (voucher.QuantityAvailable.HasValue)
                        {
                            voucher.QuantityAvailable--;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    // For non-system-wide vouchers, user must already have it assigned
                    userVoucher = await _context.UserVouchers
                        .FirstOrDefaultAsync(uv =>
                            uv.Voucher.Code == voucherCode &&
                            uv.UserID == user.UserID &&
                            !uv.IsUsed &&
                            uv.Voucher.IsActive &&
                            uv.Voucher.ValidTo > DateTime.Now);

                    if (userVoucher == null)
                    {
                        return Json(new { success = false, message = "Mã giảm giá không thuộc về bạn hoặc đã được sử dụng" });
                    }
                }

                if (!IsVoucherValid(voucher, cart))
                {
                    return Json(new { success = false, message = "Mã giảm giá không áp dụng cho sản phẩm trong giỏ hàng" });
                }

                // Calculate all discounts and final price
                decimal totalDiscountAmount;
                decimal finalPrice = CalculateCartTotals(cart, voucher, out totalDiscountAmount);

                // Calculate individual discount amounts
                decimal productDiscountAmount = 0;
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;
                    var originalPrice = product.Price * item.Quantity;
                    var effectivePrice = product.GetEffectivePrice() * item.Quantity;
                    productDiscountAmount += originalPrice - effectivePrice;
                }

                decimal amountAfterProductDiscount = cart.TotalPrice - productDiscountAmount;
                decimal rankDiscountPercent = user.Rank?.DiscountPercent ?? 0;
                decimal rankDiscountAmount = amountAfterProductDiscount * (rankDiscountPercent / 100);
                decimal voucherDiscountAmount = totalDiscountAmount - productDiscountAmount - rankDiscountAmount;

                HttpContext.Session.SetInt32("AppliedVoucherId", voucher.VoucherID);
                HttpContext.Session.SetInt32("AppliedUserVoucherId", userVoucher.UserVoucherID);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã áp dụng mã giảm giá",
                    cart = new
                    {
                        totalPrice = cart.TotalPrice,
                        subtotal = cart.TotalPrice,
                        productDiscountAmount = productDiscountAmount,
                        rankDiscountAmount = rankDiscountAmount,
                        voucherDiscountAmount = voucherDiscountAmount,
                        totalDiscountAmount = totalDiscountAmount,
                        finalPrice = finalPrice,
                        cartItemsCount = cart.CartItems.Count
                    },
                    discountType = voucher.DiscountType,
                    discountPercent = voucher.DiscountType == "PERCENT" ? voucher.DiscountValue : 0,
                    voucherCode = voucher.Code
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying voucher {VoucherCode}", voucherCode);
                return Json(new { success = false, message = "Có lỗi xảy ra khi áp dụng mã giảm giá" });
            }
        }

        private decimal CalculateVoucherDiscount(Voucher voucher, Cart cart)
        {
            // Lấy thông tin người dùng để tính rank discount
            var user = _context.Users
                .Include(u => u.Rank)
                .FirstOrDefault(u => u.UserID == cart.UserID);

            decimal rankDiscountPercent = user?.Rank?.DiscountPercent ?? 0;

            // Tính tổng giá gốc
            decimal originalTotal = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);

            // Tính giảm giá sản phẩm
            decimal productDiscountAmount = 0;
            foreach (var item in cart.CartItems)
            {
                var product = item.Product;
                var originalPrice = product.Price * item.Quantity;
                var effectivePrice = product.GetEffectivePrice() * item.Quantity;
                productDiscountAmount += originalPrice - effectivePrice;
            }

            // Tính số tiền sau giảm giá sản phẩm
            decimal amountAfterProductDiscount = originalTotal - productDiscountAmount;

            // Tính giảm giá rank
            decimal rankDiscountAmount = amountAfterProductDiscount * (rankDiscountPercent / 100);

            // Tính số tiền sau giảm giá rank
            decimal amountAfterRankDiscount = amountAfterProductDiscount - rankDiscountAmount;

            _logger.LogInformation("Calculating Voucher Discount for {VoucherCode}. Original Total: {OriginalTotal}, " +
                "After Product Discount: {AfterProductDiscount}, After Rank Discount: {AfterRankDiscount}",
                voucher.Code, originalTotal, amountAfterProductDiscount, amountAfterRankDiscount);

            decimal voucherDiscountAmount = 0;

            if (voucher.AppliesTo == "Order")
            {
                _logger.LogInformation("Voucher {VoucherCode} applies to entire order", voucher.Code);

                // Apply discount to entire order
                if (voucher.DiscountType == "PERCENT")
                {
                    voucherDiscountAmount = amountAfterRankDiscount * (voucher.DiscountValue / 100);
                    _logger.LogInformation("Percentage Discount: {DiscountPercent}%, Amount: {DiscountAmount}",
                        voucher.DiscountValue, voucherDiscountAmount);
                }
                else if (voucher.DiscountType == "FIXED")
                {
                    voucherDiscountAmount = Math.Min(voucher.DiscountValue, amountAfterRankDiscount);
                    _logger.LogInformation("Fixed Discount: {FixedAmount}, Applied: {AppliedAmount}",
                        voucher.DiscountValue, voucherDiscountAmount);
                }
            }
            else if (voucher.AppliesTo == "Product")
            {
                _logger.LogInformation("Voucher {VoucherCode} applies to specific products", voucher.Code);

                // Apply discount to specific products only
                var applicableProductIds = voucher.VoucherProducts.Select(vp => vp.ProductID).ToList();
                _logger.LogInformation("Applicable Product IDs: {ProductIds}", string.Join(", ", applicableProductIds));

                var eligibleCartItems = cart.CartItems.Where(ci => applicableProductIds.Contains(ci.ProductID)).ToList();
                _logger.LogInformation("Eligible items in cart: {EligibleCount}", eligibleCartItems.Count);

                if (eligibleCartItems.Any())
                {
                    // Calculate eligible total after product and rank discounts
                    decimal eligibleTotal = 0;
                    foreach (var item in eligibleCartItems)
                    {
                        var product = item.Product;
                        var originalItemPrice = product.Price * item.Quantity;
                        var itemProductDiscount = originalItemPrice - (product.GetEffectivePrice() * item.Quantity);
                        var itemAfterProductDiscount = originalItemPrice - itemProductDiscount;
                        var itemRankDiscount = itemAfterProductDiscount * (rankDiscountPercent / 100);
                        var itemAfterAllDiscounts = itemAfterProductDiscount - itemRankDiscount;
                        eligibleTotal += itemAfterAllDiscounts;
                    }

                    _logger.LogInformation("Eligible total after discounts: {EligibleTotal}", eligibleTotal);

                    if (voucher.DiscountType == "PERCENT")
                    {
                        voucherDiscountAmount = eligibleTotal * (voucher.DiscountValue / 100);
                        _logger.LogInformation("Percentage Discount: {DiscountPercent}%, Amount: {DiscountAmount}",
                            voucher.DiscountValue, voucherDiscountAmount);
                    }
                    else if (voucher.DiscountType == "FIXED")
                    {
                        voucherDiscountAmount = Math.Min(voucher.DiscountValue, eligibleTotal);
                        _logger.LogInformation("Fixed Discount: {FixedAmount}, Applied: {AppliedAmount}",
                            voucher.DiscountValue, voucherDiscountAmount);
                    }
                }
            }

            _logger.LogInformation("Final Voucher Discount Amount: {DiscountAmount}", voucherDiscountAmount);
            return voucherDiscountAmount;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ClearVoucher()
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng" });
                }

                HttpContext.Session.Remove("AppliedVoucherId");
                HttpContext.Session.Remove("AppliedUserVoucherId");

                // Calculate all discounts and final price without voucher
                decimal totalDiscountAmount;
                decimal finalPrice = CalculateCartTotals(cart, null, out totalDiscountAmount);

                // Calculate individual discount amounts
                decimal productDiscountAmount = 0;
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;
                    var originalPrice = product.Price * item.Quantity;
                    var effectivePrice = product.GetEffectivePrice() * item.Quantity;
                    productDiscountAmount += originalPrice - effectivePrice;
                }

                decimal amountAfterProductDiscount = cart.TotalPrice - productDiscountAmount;
                decimal rankDiscountPercent = user.Rank?.DiscountPercent ?? 0;
                decimal rankDiscountAmount = amountAfterProductDiscount * (rankDiscountPercent / 100);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã xóa mã giảm giá",
                    cart = new
                    {
                        totalPrice = cart.TotalPrice,
                        subtotal = cart.TotalPrice,
                        productDiscountAmount = productDiscountAmount,
                        rankDiscountAmount = rankDiscountAmount,
                        voucherDiscountAmount = 0,
                        totalDiscountAmount = totalDiscountAmount,
                        finalPrice = finalPrice,
                        cartItemsCount = cart.CartItems.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing voucher for user {Email}", User.Identity.Name);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa mã giảm giá" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(int addressId, string paymentMethod)
        {
            try
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục" });
                }

                var user = await _context.Users
                    .Include(u => u.Rank)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                // Check if user's email is verified
                if (!user.EmailVerified)
                {
                    _logger.LogWarning("User {UserId} attempted checkout with unverified email", user.UserID);
                    return Json(new { success = false, message = "Vui lòng xác minh email của bạn trước khi đặt hàng", requireEmailVerification = true });
                }

                // Validate address
                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == user.UserID);
                if (address == null)
                {
                    return Json(new { success = false, message = "Địa chỉ giao hàng không hợp lệ" });
                }


                // Get cart with items
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                // Get applied voucher if exists
                int? appliedVoucherId = HttpContext.Session.GetInt32("AppliedVoucherId");
                int? appliedUserVoucherId = HttpContext.Session.GetInt32("AppliedUserVoucherId");
                Voucher appliedVoucher = null;
                UserVoucher userVoucher = null;

                if (appliedVoucherId.HasValue)
                {
                    appliedVoucher = await _context.Vouchers
                        .Include(v => v.VoucherProducts)
                        .FirstOrDefaultAsync(v => v.VoucherID == appliedVoucherId.Value);

                    if (appliedVoucher != null)
                    {
                        _logger.LogInformation("Found applied voucher for checkout: {VoucherCode}", appliedVoucher.Code);

                        // Get the associated UserVoucher record
                        if (appliedUserVoucherId.HasValue)
                        {
                            userVoucher = await _context.UserVouchers.FindAsync(appliedUserVoucherId.Value);
                        }
                        else
                        {
                            // Fallback if we somehow don't have the UserVoucherId in session
                            userVoucher = await _context.UserVouchers
                                .FirstOrDefaultAsync(uv =>
                                    uv.VoucherID == appliedVoucher.VoucherID &&
                                    uv.UserID == user.UserID &&
                                    !uv.IsUsed);
                        }

                        // Verify the voucher is still valid
                        if (userVoucher == null || userVoucher.IsUsed ||
                            !appliedVoucher.IsActive || appliedVoucher.ValidTo <= DateTime.Now)
                        {
                            return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã được sử dụng" });
                        }
                    }
                }

                // Calculate all discounts and final price
                decimal totalDiscountAmount;
                decimal finalPrice = CalculateCartTotals(cart, appliedVoucher, out totalDiscountAmount);

                // Calculate individual discount amounts
                decimal productDiscountAmount = 0;
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;
                    var originalPrice = product.Price * item.Quantity;
                    var effectivePrice = product.GetEffectivePrice() * item.Quantity;
                    productDiscountAmount += originalPrice - effectivePrice;
                }

                decimal amountAfterProductDiscount = cart.TotalPrice - productDiscountAmount;
                decimal rankDiscountPercent = user.Rank?.DiscountPercent ?? 0;
                decimal rankDiscountAmount = amountAfterProductDiscount * (rankDiscountPercent / 100);
                decimal voucherDiscountAmount = totalDiscountAmount - productDiscountAmount - rankDiscountAmount;

                // Create order with all discount information
                var (checkoutSuccess, checkoutMessage, orderId) = await _cartService.CheckoutAsync(
                    user.UserID,
                    addressId,
                    paymentMethod,
                    appliedVoucherId);  // Pass the voucher ID

                if (!checkoutSuccess)
                {
                    return Json(new { success = false, message = checkoutMessage });
                }

                // Mark the user voucher as used if applicable
                if (userVoucher != null && orderId.HasValue)
                {
                    userVoucher.IsUsed = true;
                    userVoucher.UsedDate = DateTime.Now;
                    userVoucher.OrderID = orderId.Value;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Marked user voucher {UserVoucherId} as used for order {OrderId}", userVoucher.UserVoucherID, orderId.Value);
                }

                // Clear voucher from session
                HttpContext.Session.Remove("AppliedVoucherId");
                HttpContext.Session.Remove("AppliedUserVoucherId");

                // Handle payment method
                if (paymentMethod == "VNPay")
                {
                    return Json(new
                    {
                        success = true,
                        orderId,
                        paymentMethod,
                        message = "Đặt hàng thành công. Vui lòng tiến hành thanh toán."
                    });
                }
                else if (paymentMethod == "COD")
                {
                    if (orderId.HasValue)
                    {
                        try
                        {
                            // Gọi API để xử lý thanh toán COD
                            var httpClient = new System.Net.Http.HttpClient();
                            _logger.LogCritical("Calling API to process COD payment for order {OrderId}", orderId.Value);
                            // Đảm bảo rằng bạn đã cấu hình đường dẫn API đúng (ví dụ: "Calling API to process COD payment for order {OrderId}", orderId.Value);
                            var response = await httpClient.PostAsync(
                                $"http://localhost:5246/Payment/ProcessCODPayment?orderId={orderId.Value}",
                                new System.Net.Http.StringContent("", System.Text.Encoding.UTF8, "application/json"));

                            if (response.IsSuccessStatusCode)
                            {
                                return Json(new
                                {
                                    success = true,
                                    message = "Đặt hàng thành công. Cảm ơn bạn đã mua hàng!"
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "Có lỗi xảy ra khi xử lý đơn hàng COD. Vui lòng thử lại."
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing COD payment for order {OrderId}", orderId.Value);
                            return Json(new
                            {
                                success = false,
                                message = "Có lỗi xảy ra khi xử lý đơn hàng COD. Vui lòng thử lại."
                            });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy thông tin đơn hàng" });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                return Json(new { success = false, message = "Có lỗi xảy ra trong quá trình đặt hàng" });
            }
        }

        private bool IsVoucherValid(Voucher voucher, Cart cart)
        {
            // Check if voucher is active and within valid date range
            if (!voucher.IsActive || voucher.ValidFrom > DateTime.Now || voucher.ValidTo < DateTime.Now)
            {
                _logger.LogWarning("Voucher {VoucherCode} is not active or outside date range", voucher.Code);
                return false;
            }

            // Check if voucher has available quantity
            if (voucher.QuantityAvailable.HasValue && voucher.QuantityAvailable <= 0)
            {
                _logger.LogWarning("Voucher {VoucherCode} has no available quantity", voucher.Code);
                return false;
            }

            // Check if voucher applies to the entire order
            if (voucher.AppliesTo == "Order")
            {
                _logger.LogInformation("Voucher {VoucherCode} applies to entire order", voucher.Code);
                return true;
            }

            // Check if voucher applies to specific products in the cart
            if (voucher.AppliesTo == "Product" && voucher.VoucherProducts != null && voucher.VoucherProducts.Any())
            {
                var applicableProductIds = voucher.VoucherProducts.Select(vp => vp.ProductID).ToList();
                var hasMatchingProducts = cart.CartItems.Any(ci => applicableProductIds.Contains(ci.ProductID));

                _logger.LogInformation("Voucher {VoucherCode} applies to specific products. Has matching products: {HasMatching}",
                    voucher.Code, hasMatchingProducts);

                return hasMatchingProducts;
            }

            _logger.LogWarning("Voucher {VoucherCode} is not valid for this cart", voucher.Code);
            return false;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTestVoucher(bool isSystemWide, string code, string discountType, decimal discountValue, string appliesTo)
        {
            try
            {
                // This method is for development/testing only
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Create the voucher
                var voucher = new Voucher
                {
                    Code = code,
                    Description = $"Test voucher created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    DiscountType = discountType,
                    DiscountValue = discountValue,
                    QuantityAvailable = isSystemWide ? 100 : 1, // System-wide vouchers have more quantity
                    ValidFrom = DateTime.Now,
                    ValidTo = DateTime.Now.AddDays(7), // Valid for 7 days
                    IsActive = true,
                    AppliesTo = appliesTo,
                    IsSystemWide = isSystemWide
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();

                // If it's a user-specific voucher, assign it to the current user
                if (!isSystemWide)
                {
                    var userVoucher = new UserVoucher
                    {
                        UserID = user.UserID,
                        VoucherID = voucher.VoucherID,
                        AssignedDate = DateTime.Now,
                        IsUsed = false
                    };

                    _context.UserVouchers.Add(userVoucher);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"User-specific voucher '{code}' created and assigned to you. Check your Vouchers page to use it."
                    });
                }

                // For system-wide vouchers, just return success
                return Json(new
                {
                    success = true,
                    message = $"System-wide voucher '{code}' created. You can apply it directly in the cart."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test voucher");
                return Json(new { success = false, message = "Error creating test voucher" });
            }
        }
    }
}