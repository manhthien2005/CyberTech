using CyberTech.Models;
using CyberTech.Models.DTOs;
using CyberTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace CyberTech.Controllers
{
    [Authorize(Roles = "Admin,Staff,Support,Manager,SuperAdmin")]
    [Route("VoucherManage")]
    public class VoucherManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVoucherService _voucherService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public VoucherManageController(ApplicationDbContext context, IVoucherService voucherService, IUserService userService, IEmailService emailService)
        {
            _context = context;
            _voucherService = voucherService;
            _userService = userService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetVouchers")]
        public async Task<IActionResult> GetVouchers(int page = 1, int pageSize = 5, string searchTerm = "", string sortBy = "", string startDate = null, string endDate = null)
        {
            try
            {
                DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
                DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);
                var result = await _voucherService.GetVouchersAsync(page, pageSize, searchTerm, sortBy, start, end);
                return Json(new { success = true, data = result.vouchers, pagination = result.pagination });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách voucher: " + ex.Message });
            }
        }

        [HttpPost("CreateVoucher")]
        public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherDTO voucherDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }

                var result = await _voucherService.CreateVoucherAsync(voucherDto);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tạo voucher mới" });
            }
        }

        [HttpPost("UpdateVoucher")]
        public async Task<IActionResult> UpdateVoucher([FromBody] UpdateVoucherDTO voucherDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }

                var result = await _voucherService.UpdateVoucherAsync(voucherDto);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("DeleteVoucher")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            try
            {
                var result = await _voucherService.DeleteVoucherAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("SendVoucherToUser")]
        public async Task<IActionResult> SendVoucherToUser(int voucherId, int userId)
        {
            try
            {
                var voucher = await _context.Vouchers.FindAsync(voucherId);
                if (voucher == null)
                    return Json(new { success = false, message = "Không tìm thấy voucher" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });

                // Check if user already has an unused voucher of this type
                var existingVoucher = await _context.UserVouchers
                    .FirstOrDefaultAsync(uv => uv.UserID == userId && uv.VoucherID == voucherId && !uv.IsUsed);

                if (existingVoucher != null)
                    return Json(new { success = false, message = "Người dùng đã có voucher này và chưa sử dụng" });

                var result = await _userService.AssignVoucherToUserAsync(userId, voucherId);
                if (result)
                {
                    // Send email notification
                    string subject = "Voucher mới từ CyberTech";
                    string body = $"<p>Xin chào {user.Name},</p><p>Bạn đã nhận được một voucher mới!</p><p>Mã: <strong>{voucher.Code}</strong></p><p>Giá trị: {(voucher.DiscountType == "PERCENT" ? voucher.DiscountValue + "%" : voucher.DiscountValue.ToString("C", new System.Globalization.CultureInfo("vi-VN")))}</p><p>Hiệu lực đến: {voucher.ValidTo.ToString("dd/MM/yyyy")}</p><p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>";
                    await _emailService.SendEmailAsync(user.Email, subject, body);

                    return Json(new { success = true, message = "Đã gửi voucher cho người dùng" });
                }
                return Json(new { success = false, message = "Không thể gửi voucher" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi gửi voucher: " + ex.Message });
            }
        }

        [HttpPost("SendVoucherToAllUsers")]
        public async Task<IActionResult> SendVoucherToAllUsers(int voucherId)
        {
            try
            {
                var voucher = await _context.Vouchers.FindAsync(voucherId);
                if (voucher == null)
                    return Json(new { success = false, message = "Không tìm thấy voucher" });

                var users = await _context.Users.ToListAsync();
                int successCount = 0;
                int failedCount = 0;
                List<string> errorEmails = new List<string>();
                List<string> alreadyHaveVoucher = new List<string>();

                // Process voucher assignment for each user
                foreach (var user in users)
                {
                    // Check if user already has an unused voucher of this type
                    var existingVoucher = await _context.UserVouchers
                        .FirstOrDefaultAsync(uv => uv.UserID == user.UserID && uv.VoucherID == voucherId && !uv.IsUsed);

                    if (existingVoucher != null)
                    {
                        alreadyHaveVoucher.Add(user.Email);
                        failedCount++;
                        continue;
                    }

                    if (await _userService.AssignVoucherToUserAsync(user.UserID, voucherId))
                    {
                        try
                        {
                            // Send email notification
                            string subject = "Voucher mới từ CyberTech";
                            string body = $"<p>Xin chào {user.Name},</p><p>Bạn đã nhận được một voucher mới!</p><p>Mã: <strong>{voucher.Code}</strong></p><p>Giá trị: {(voucher.DiscountType == "PERCENT" ? voucher.DiscountValue + "%" : voucher.DiscountValue.ToString("C", new System.Globalization.CultureInfo("vi-VN")))}</p><p>Hiệu lực đến: {voucher.ValidTo.ToString("dd/MM/yyyy")}</p><p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>";
                            await _emailService.SendEmailAsync(user.Email, subject, body);
                            successCount++;
                        }
                        catch (Exception emailEx)
                        {
                            // Log email sending failure but continue processing
                            errorEmails.Add($"{user.Email}: {emailEx.Message}");
                            failedCount++;
                        }
                    }
                    else
                    {
                        failedCount++;
                    }
                }

                string message = $"Đã gửi voucher cho {successCount} người dùng";
                if (failedCount > 0)
                    message += $", thất bại với {failedCount} người dùng";
                if (errorEmails.Count > 0)
                    message += $", lỗi gửi email cho {errorEmails.Count} người dùng";
                if (alreadyHaveVoucher.Count > 0)
                    message += $", {alreadyHaveVoucher.Count} người dùng đã có voucher và chưa sử dụng";

                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi gửi voucher cho tất cả người dùng: " + ex.Message });
            }
        }

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers(string searchTerm = "")
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u => u.Name.ToLower().Contains(searchTerm) || u.Email.ToLower().Contains(searchTerm));
            }

            var users = await query.Take(10).Select(u => new { id = u.UserID, name = u.Name, email = u.Email }).ToListAsync();
            return Json(users);
        }

        [HttpGet("GetProducts")]
        public async Task<IActionResult> GetProducts(string searchTerm = "")
        {
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchTerm));
            }

            var products = await query.Take(10).Select(p => new { id = p.ProductID, name = p.Name }).ToListAsync();
            return Json(products);
        }
    }
}