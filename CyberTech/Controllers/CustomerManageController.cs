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
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace CyberTech.Controllers
{
    [Authorize(Roles = "Admin,Staff,Support,Manager,SuperAdmin")]
    [Route("CustomerManage")]
    public class CustomerManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IVoucherService _voucherService;
        private readonly IEmailService _emailService;

        public CustomerManageController(ApplicationDbContext context, IUserService userService, IVoucherService voucherService, IEmailService emailService)
        {
            _context = context;
            _userService = userService;
            _voucherService = voucherService;
            _emailService = emailService;
        }

        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return true; // Phone is optional
            // Kiểm tra số điện thoại Việt Nam
            var phoneRegex = new Regex(@"^(0|\+84)([0-9]{9,10})$");
            return phoneRegex.IsMatch(phone.Trim());
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetCustomers")]
        public async Task<IActionResult> GetCustomers(int page = 1, int pageSize = 10, string searchTerm = "", string sortBy = "", string status = "")
        {
            try
            {
                var query = _context.Users
                    .Where(u => u.Role == "Customer")
                    .Include(u => u.Rank)
                    .AsQueryable();

                // Apply search
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(u => u.Name.ToLower().Contains(searchTerm) ||
                                          u.Email.ToLower().Contains(searchTerm) ||
                                          u.Username.ToLower().Contains(searchTerm) ||
                                          (u.Phone != null && u.Phone.Contains(searchTerm)));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    // Ensure status is one of the valid values
                    if (status == "Active" || status == "Inactive" || status == "Suspended")
                    {
                        query = query.Where(u => u.UserStatus == status);
                    }
                }

                // Apply sorting
                query = sortBy switch
                {
                    "name_asc" => query.OrderBy(u => u.Name),
                    "name_desc" => query.OrderByDescending(u => u.Name),
                    "email_asc" => query.OrderBy(u => u.Email),
                    "email_desc" => query.OrderByDescending(u => u.Email),
                    "date_asc" => query.OrderBy(u => u.CreatedAt),
                    "date_desc" => query.OrderByDescending(u => u.CreatedAt),
                    "spent_asc" => query.OrderBy(u => u.TotalSpent),
                    "spent_desc" => query.OrderByDescending(u => u.TotalSpent),
                    _ => query.OrderByDescending(u => u.CreatedAt)
                };

                // Calculate pagination
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var customers = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new CustomerViewModel
                    {
                        UserId = u.UserID,
                        Name = u.Name,
                        Username = u.Username,
                        Email = u.Email,
                        Phone = u.Phone,
                        ProfileImageURL = u.ProfileImageURL,
                        TotalSpent = u.TotalSpent,
                        OrderCount = u.OrderCount,
                        RankName = u.Rank.RankName,
                        RankId = u.RankId.Value,
                        Status = u.UserStatus,
                        CreatedAt = u.CreatedAt,
                        Gender = u.Gender,
                        DateOfBirth = u.DateOfBirth
                    })
                    .ToListAsync();

                var pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalItems,
                    totalPages
                };

                return Json(new { success = true, data = customers, pagination });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách khách hàng: " + ex.Message });
            }
        }

        [HttpGet("GetCustomer/{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            try
            {
                var customer = await _context.Users
                    .Include(u => u.Rank)
                    .Where(u => u.UserID == id && u.Role == "Customer")
                    .Select(u => new CustomerViewModel
                    {
                        UserId = u.UserID,
                        Name = u.Name,
                        Username = u.Username,
                        Email = u.Email,
                        Phone = u.Phone,
                        ProfileImageURL = u.ProfileImageURL,
                        TotalSpent = u.TotalSpent,
                        OrderCount = u.OrderCount,
                        RankName = u.Rank.RankName,
                        RankId = u.RankId.Value,
                        Status = u.UserStatus,
                        CreatedAt = u.CreatedAt,
                        Gender = u.Gender,
                        DateOfBirth = u.DateOfBirth
                    })
                    .FirstOrDefaultAsync();

                if (customer == null)
                    return Json(new { success = false, message = "Không tìm thấy khách hàng" });

                return Json(new { success = true, data = customer });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải thông tin khách hàng: " + ex.Message });
            }
        }

        [HttpGet("GetCustomerAddresses/{userId}")]
        public async Task<IActionResult> GetCustomerAddresses(int userId)
        {
            try
            {
                var addresses = await _context.UserAddresses
                    .Where(a => a.UserID == userId)
                    .OrderByDescending(a => a.IsPrimary)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToListAsync();

                return Json(new { success = true, data = addresses });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải địa chỉ khách hàng: " + ex.Message });
            }
        }

        [HttpGet("GetCustomerVouchers/{userId}")]
        public async Task<IActionResult> GetCustomerVouchers(int userId)
        {
            try
            {
                var vouchers = await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .Where(uv => uv.UserID == userId)
                    .Select(uv => new UserVoucherViewModel
                    {
                        UserVoucherId = uv.UserVoucherID,
                        VoucherId = uv.VoucherID,
                        Code = uv.Voucher.Code,
                        Description = uv.Voucher.Description,
                        DiscountType = uv.Voucher.DiscountType,
                        DiscountValue = uv.Voucher.DiscountValue,
                        ValidFrom = uv.Voucher.ValidFrom,
                        ValidTo = uv.Voucher.ValidTo,
                        IsUsed = uv.IsUsed,
                        AssignedDate = uv.AssignedDate,
                        UsedDate = uv.UsedDate,
                        OrderId = uv.OrderID
                    })
                    .OrderByDescending(v => v.AssignedDate)
                    .ToListAsync();

                return Json(new { success = true, data = vouchers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải voucher của khách hàng: " + ex.Message });
            }
        }

        [HttpGet("GetCustomerOrders/{userId}")]
        public async Task<IActionResult> GetCustomerOrders(int userId)
        {
            try
            {
                var orders = await _context.Orders
                    .Where(o => o.UserID == userId)
                    .Select(o => new OrderViewModel
                    {
                        OrderId = o.OrderID,
                        TotalPrice = o.TotalPrice,
                        FinalPrice = o.FinalPrice,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        TotalDiscountAmount = o.TotalDiscountAmount,
                        ItemCount = o.OrderItems.Count
                    })
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                return Json(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải đơn hàng của khách hàng: " + ex.Message });
            }
        }

        [HttpGet("GetOrderDetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.UserAddress)
                    .Where(o => o.OrderID == orderId)
                    .Select(o => new OrderDetailViewModel
                    {
                        OrderId = o.OrderID,
                        TotalPrice = o.TotalPrice,
                        FinalPrice = o.FinalPrice,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        TotalDiscountAmount = o.TotalDiscountAmount,
                        RankDiscountAmount = o.RankDiscountAmount,
                        VoucherDiscountAmount = o.VoucherDiscountAmount,
                        ProductDiscountAmount = o.ProductDiscountAmount,
                        RecipientName = o.UserAddress.RecipientName,
                        RecipientPhone = o.UserAddress.Phone,
                        ShippingAddress = $"{o.UserAddress.AddressLine}, {o.UserAddress.Ward}, {o.UserAddress.District}, {o.UserAddress.City}",
                        OrderItems = o.OrderItems.Select(oi => new OrderItemViewModel
                        {
                            OrderItemId = oi.OrderItemID,
                            ProductId = oi.ProductID,
                            ProductName = oi.Product.Name,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            Subtotal = oi.Subtotal,
                            DiscountAmount = oi.DiscountAmount,
                            FinalSubtotal = oi.FinalSubtotal
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                return Json(new { success = true, data = order });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải chi tiết đơn hàng: " + ex.Message });
            }
        }

        [HttpPost("AssignVoucher")]
        public async Task<IActionResult> AssignVoucher([FromBody] AssignVoucherRequest request)
        {
            try
            {
                if (request == null || request.UserId <= 0 || request.VoucherId <= 0)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

                var result = await _userService.AssignVoucherToUserAsync(request.UserId, request.VoucherId);

                if (result)
                {
                    var user = await _context.Users.FindAsync(request.UserId);
                    var voucher = await _context.Vouchers.FindAsync(request.VoucherId);

                    if (user != null && voucher != null)
                    {
                        string subject = "Voucher mới từ Happy Kitchen";
                        string body = $"<p>Xin chào {user.Name},</p><p>Bạn đã nhận được một voucher mới!</p><p>Mã: <strong>{voucher.Code}</strong></p><p>Giá trị: {(voucher.DiscountType == "PERCENT" ? voucher.DiscountValue + "%" : voucher.DiscountValue.ToString("C", new System.Globalization.CultureInfo("vi-VN")))}</p><p>Hiệu lực đến: {voucher.ValidTo.ToString("dd/MM/yyyy")}</p><p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>";
                        await _emailService.SendEmailAsync(user.Email, subject, body);
                    }

                    return Json(new { success = true, message = "Đã gán voucher cho khách hàng thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể gán voucher cho khách hàng" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi gán voucher: " + ex.Message });
            }
        }

        [HttpPost("RemoveVoucher")]
        public async Task<IActionResult> RemoveVoucher([FromBody] RemoveVoucherRequest request)
        {
            try
            {
                if (request == null || request.UserVoucherId <= 0)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

                var userVoucher = await _context.UserVouchers.FindAsync(request.UserVoucherId);
                if (userVoucher == null)
                    return Json(new { success = false, message = "Không tìm thấy voucher" });

                if (userVoucher.IsUsed)
                    return Json(new { success = false, message = "Không thể xóa voucher đã sử dụng" });

                _context.UserVouchers.Remove(userVoucher);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa voucher khỏi khách hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa voucher: " + ex.Message });
            }
        }

        [HttpPost("UpdateCustomer")]
        public async Task<IActionResult> UpdateCustomer([FromBody] UpdateCustomerRequest request)
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

                if (!string.IsNullOrEmpty(request.Phone) && !IsValidPhoneNumber(request.Phone))
                {
                    return Json(new { success = false, message = "Số điện thoại không hợp lệ" });
                }

                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null || user.Role != "Customer")
                    return Json(new { success = false, message = "Không tìm thấy khách hàng" });

                user.Name = request.Name;
                user.Phone = request.Phone?.Trim();
                user.Gender = request.Gender;
                user.DateOfBirth = request.DateOfBirth;
                user.UserStatus = request.Status;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật thông tin khách hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật thông tin khách hàng: " + ex.Message });
            }
        }

        [HttpPost("CreateCustomer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
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

                if (!string.IsNullOrEmpty(request.Phone) && !IsValidPhoneNumber(request.Phone))
                {
                    return Json(new { success = false, message = "Số điện thoại không hợp lệ" });
                }

                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
                if (emailExists)
                    return Json(new { success = false, message = "Email đã tồn tại trong hệ thống" });

                var usernameExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
                if (usernameExists)
                    return Json(new { success = false, message = "Tên đăng nhập đã tồn tại trong hệ thống" });

                var result = await _userService.RegisterAsync(
                    request.Name,
                    request.Username,
                    request.Email,
                    request.Password
                );

                if (result)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
                    if (user != null)
                    {
                        user.Phone = request.Phone?.Trim();
                        await _context.SaveChangesAsync();
                    }
                    return Json(new { success = true, message = "Tạo khách hàng mới thành công" });
                }
                else
                    return Json(new { success = false, message = "Không thể tạo khách hàng mới" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tạo khách hàng mới: " + ex.Message });
            }
        }

        [HttpGet("GetAvailableVouchers")]
        public async Task<IActionResult> GetAvailableVouchers(int userId)
        {
            try
            {
                // Get all active vouchers
                var availableVouchers = await _context.Vouchers
                    .Where(v => v.IsActive && v.ValidTo > DateTime.Now)
                    .ToListAsync();

                // Get user's current vouchers
                var userVoucherIds = await _context.UserVouchers
                    .Where(uv => uv.UserID == userId && !uv.IsUsed)
                    .Select(uv => uv.VoucherID)
                    .ToListAsync();

                // Filter out vouchers the user already has
                var vouchers = availableVouchers
                    .Where(v => !userVoucherIds.Contains(v.VoucherID))
                    .Select(v => new VoucherViewModel
                    {
                        VoucherId = v.VoucherID,
                        Code = v.Code,
                        Description = v.Description,
                        DiscountType = v.DiscountType,
                        DiscountValue = v.DiscountValue,
                        ValidFrom = v.ValidFrom,
                        ValidTo = v.ValidTo,
                        IsActive = v.IsActive,
                        AppliesTo = v.AppliesTo
                    })
                    .ToList();

                return Json(new { success = true, data = vouchers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách voucher: " + ex.Message });
            }
        }

        [HttpGet("GetRanks")]
        public async Task<IActionResult> GetRanks()
        {
            try
            {
                var ranks = await _context.Ranks
                    .OrderBy(r => r.PriorityLevel)
                    .Select(r => new
                    {
                        r.RankId,
                        r.RankName,
                        r.MinTotalSpent,
                        r.DiscountPercent,
                        r.PriorityLevel
                    })
                    .ToListAsync();

                return Json(new { success = true, data = ranks });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể tải danh sách cấp bậc: " + ex.Message });
            }
        }
    }

    // DTOs for Customer Management
    public class CustomerViewModel
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string ProfileImageURL { get; set; }
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public string RankName { get; set; }
        public int RankId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }

    public class UserVoucherViewModel
    {
        public int UserVoucherId { get; set; }
        public int VoucherId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsUsed { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? UsedDate { get; set; }
        public int? OrderId { get; set; }
    }

    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public int ItemCount { get; set; }
    }

    public class OrderDetailViewModel
    {
        public int OrderId { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal RankDiscountAmount { get; set; }
        public decimal VoucherDiscountAmount { get; set; }
        public decimal ProductDiscountAmount { get; set; }
        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string ShippingAddress { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }
    }

    public class OrderItemViewModel
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalSubtotal { get; set; }
    }

    public class AssignVoucherRequest
    {
        public int UserId { get; set; }
        public int VoucherId { get; set; }
    }

    public class RemoveVoucherRequest
    {
        public int UserVoucherId { get; set; }
    }

    public class UpdateCustomerRequest
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public byte? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Status { get; set; }
    }

    public class CreateCustomerRequest
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
    }
}