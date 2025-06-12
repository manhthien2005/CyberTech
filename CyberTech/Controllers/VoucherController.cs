using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CyberTech.Controllers
{
    public class VoucherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<VoucherController> _logger;
        private readonly IVoucherTokenService _voucherTokenService;

        public VoucherController(
            ApplicationDbContext context,
            IUserService userService,
            ILogger<VoucherController> logger,
            IVoucherTokenService voucherTokenService)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
            _voucherTokenService = voucherTokenService;
        }

        [HttpGet]
        [Authorize]
        [Route("voucher/claim")]
        public async Task<IActionResult> Claim(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Voucher token is empty");
                    TempData["ErrorMessage"] = "Đường dẫn không hợp lệ.";
                    return RedirectToAction("Index", "Home");
                }

                // Validate the token
                var (isValid, message, voucherToken) = await _voucherTokenService.ValidateTokenAsync(token);
                if (!isValid || voucherToken == null)
                {
                    _logger.LogWarning("Invalid voucher token: {Token}, Message: {Message}", token, message);
                    TempData["ErrorMessage"] = message;
                    return RedirectToAction("Index", "Home");
                }

                // Get the current user
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                    return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Claim", "Voucher", new { token }) });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    _logger.LogError("User not found for email: {Email}", emailClaim.Value);
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                    return RedirectToAction("Login", "Account");
                }

                // Check if the token belongs to this user
                if (voucherToken.UserID != user.UserID)
                {
                    _logger.LogWarning("Token {Token} belongs to user {TokenUserId} but current user is {CurrentUserId}",
                        token, voucherToken.UserID, user.UserID);
                    TempData["ErrorMessage"] = "Voucher này không thuộc về bạn.";
                    return RedirectToAction("Index", "Home");
                }

                // Create or get the voucher
                var voucherCode = voucherToken.VoucherCode;
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == voucherCode);

                if (voucher == null)
                {
                    // Create a new voucher based on the voucher code
                    if (voucherCode == "USERPROMO50")
                    {
                        voucher = new Voucher
                        {
                            Code = voucherCode,
                            Description = "Voucher giảm giá 50.000đ cho đơn hàng",
                            DiscountType = "FIXED",
                            DiscountValue = 50000, // 50,000 VND discount
                            ValidFrom = DateTime.Now,
                            ValidTo = DateTime.Now.AddDays(7),
                            IsActive = true,
                            AppliesTo = "Order",
                            IsSystemWide = false
                        };
                    }
                    else if (voucherCode == "PREMIUM10")
                    {
                        voucher = new Voucher
                        {
                            Code = voucherCode,
                            Description = "Voucher giảm giá 10% cho đơn hàng",
                            DiscountType = "PERCENT",
                            DiscountValue = 10, // 10% discount
                            ValidFrom = DateTime.Now,
                            ValidTo = DateTime.Now.AddDays(14), // 14 days validity for premium vouchers
                            IsActive = true,
                            AppliesTo = "Order",
                            IsSystemWide = false
                        };
                    }
                    else
                    {
                        // Default voucher if code is not recognized
                        voucher = new Voucher
                        {
                            Code = voucherCode,
                            Description = "Voucher giảm giá",
                            DiscountType = "FIXED",
                            DiscountValue = 50000,
                            ValidFrom = DateTime.Now,
                            ValidTo = DateTime.Now.AddDays(7),
                            IsActive = true,
                            AppliesTo = "Order",
                            IsSystemWide = false
                        };
                    }

                    _context.Vouchers.Add(voucher);
                    await _context.SaveChangesAsync();
                }

                // Check if the user already has this voucher
                var existingUserVoucher = await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .FirstOrDefaultAsync(uv =>
                        uv.UserID == user.UserID &&
                        uv.VoucherID == voucher.VoucherID &&
                        !uv.IsUsed &&
                        uv.Voucher.ValidTo > DateTime.Now);

                if (existingUserVoucher != null)
                {
                    // Mark the token as used even though we didn't create a new voucher
                    await _voucherTokenService.MarkTokenAsUsedAsync(token);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} already has voucher {VoucherCode}", user.UserID, voucherCode);
                    TempData["WarningMessage"] = "Bạn đã có voucher này trong kho voucher của mình.";
                    return RedirectToAction("Vouchers", "Account");
                }

                // Check if the user has any other active tokens for this voucher code
                var otherTokens = await _context.VoucherTokens
                    .Where(vt =>
                        vt.UserID == user.UserID &&
                        vt.VoucherCode == voucherCode &&
                        vt.TokenID != voucherToken.TokenID &&
                        !vt.IsUsed &&
                        vt.ExpiresAt > DateTime.Now)
                    .ToListAsync();

                // Mark other tokens as used to prevent duplicate vouchers
                if (otherTokens.Any())
                {
                    foreach (var otherToken in otherTokens)
                    {
                        otherToken.IsUsed = true;
                        otherToken.UsedAt = DateTime.Now;
                    }
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Marked {Count} other tokens as used for user {UserId}", otherTokens.Count, user.UserID);
                }

                // Assign the voucher to the user
                var userVoucher = new UserVoucher
                {
                    UserID = user.UserID,
                    VoucherID = voucher.VoucherID,
                    AssignedDate = DateTime.Now,
                    IsUsed = false
                };

                _context.UserVouchers.Add(userVoucher);

                // Mark the token as used
                await _voucherTokenService.MarkTokenAsUsedAsync(token);

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} successfully claimed voucher {VoucherCode} with token {Token}",
                    user.UserID, voucherCode, token);

                // Set appropriate success message based on voucher type
                if (voucherCode == "PREMIUM10")
                {
                    TempData["SuccessMessage"] = "Chúc mừng! Bạn đã nhận được voucher giảm giá 10% cho đơn hàng tiếp theo.";
                }
                else if (voucherCode == "USERPROMO50")
                {
                    TempData["SuccessMessage"] = "Chúc mừng! Bạn đã nhận được voucher giảm giá 50.000đ cho đơn hàng tiếp theo.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Chúc mừng! Bạn đã nhận được voucher giảm giá cho đơn hàng tiếp theo.";
                }

                return RedirectToAction("Vouchers", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming voucher with token {Token}", token);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi nhận voucher. Vui lòng thử lại sau.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}